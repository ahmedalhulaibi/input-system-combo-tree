using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ComboTreeInputSystem
{
    public class ComboTreeInput : MonoBehaviour
    {
        [SerializeField] private ComboTree comboTreeData;
        [SerializeField] private float comboTimeoutSeconds = 0.6f;
        [SerializeField] private float comboTimeoutTimer = 0.0f;

        private InputActionReference[] uniqueInputActionReferences;
        private InputActionReference[] comboInputActionReferences;
        private Dictionary<InputActionReference, ComboTree.NTree<InputActionReference>> comboTrees;
        private ComboTree.NTree<InputActionReference> currentComboTree = null;
        private void OnEnable()
        {
            comboTrees = comboTreeData.GetComboTrees();
            uniqueInputActionReferences = comboTreeData.GetUniqueInputActionReferences();
            comboInputActionReferences = comboTreeData.GetGeneratedComboInputActionReferences();
            foreach (var i in uniqueInputActionReferences)
            {
                i.action.Enable();
                i.action.performed += _ => HandleAction(i);
            }
            foreach (var i in comboInputActionReferences)
            {
                i.action.Enable();
            }
        }

        private void OnDisable()
        {
            foreach (var i in uniqueInputActionReferences)
            {
                i.action.performed -= _ => HandleAction(i);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            comboTimeoutTimer += Time.deltaTime;
            if (comboTimeoutTimer >= comboTimeoutSeconds)
            {
                ResetComboTracker();
                ResetTimers();
            }
        }

        void HandleAction(InputActionReference inputActionReference)
        {
            Debug.Log("HandleAction " + inputActionReference.name);
            if (currentComboTree == null)
            {
                currentComboTree = comboTrees[inputActionReference];
                ResetTimers();

                return;
            }
            Debug.Log("currentComboTree " + currentComboTree.data.name);

            ComboTree.NTree<InputActionReference> nextSubTree = null;
            var j = 1;
            var maybeSubTree = currentComboTree.GetChild(j);
            while (maybeSubTree != null)
            {
                if (maybeSubTree.data == inputActionReference)
                {
                    nextSubTree = maybeSubTree;
                    ResetTimers();
                    break;
                }
                j++;
                maybeSubTree = currentComboTree.GetChild(j);
            }

            if (nextSubTree == null)
            {
                ResetComboTracker();
            }
            currentComboTree = nextSubTree;
            checkIfComboDetected();
        }

        private void checkIfComboDetected()
        {
            if (currentComboTree == null) { return; }
            Debug.Log("checkIfComboDetected");
            var recognized = false;
            var j = 1;
            while (!recognized)
            {
                var maybeCombo = currentComboTree.GetChild(j);
                if (maybeCombo == null) { break; }
                Debug.Log("checkIfComboDetected " + maybeCombo.data.name);
                if (comboInputActionReferences.Contains(maybeCombo.data))
                {
                    Debug.Log("combo detected! " + maybeCombo.data.name);
                    //var del = maybeCombo.data.action.;
                    // TODO: combo detected
                    SimulatedComboDeviceMessageBus.TriggerCombo(maybeCombo.data);
                    ResetComboTracker();
                    recognized = true;
                }

                j++;
            }
        }

        public void ResetTimers()
        {
            comboTimeoutTimer = 0;
        }

        public void ResetComboTracker()
        {
            currentComboTree = null;
            ResetTimers();
        }
    }
}
