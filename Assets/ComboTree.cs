using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ComboTreeInputSystem
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ComboTree", order = 1)]
    public class ComboTree : ScriptableObject
    {
        private const string COMBO_ACTION_MAP_NAME = "Combos";

        // A new action map will be added here called Combos with new input action references added
        public InputActionAsset inputActionAsset;
        [Serializable]
        public struct Sequence
        {
            public string name;
            public InputActionReference[] inputActions;
        }
        [SerializeField] Sequence[] sequences;

        private Dictionary<InputActionReference, InputActionReference> uniqueInputActionReferences = new Dictionary<InputActionReference, InputActionReference>();
        private Dictionary<InputActionReference, NTree<InputActionReference>> sequenceTrees = null;
        private Dictionary<string, InputActionReference> generatedComboInputActionReferences = new Dictionary<string, InputActionReference>();

        private void OnEnable()
        {
        }

        private void OnValidate()
        {
            foreach (var sequence in sequences)
            {
                if (!generatedComboInputActionReferences.TryGetValue(sequence.name, out var val))
                {
                    Debug.LogWarning("sequence " + sequence.name + " does not have an input action. Make sure to click 'Update Input Action Map' and 'Generate Simulated Combo Device' whenever you add a new combo sequence");
                }
            }
        }

        public void UpdateInputActionMap()
        {
            if (inputActionAsset == null)
            {
                throw new Exception("Please provide an input action asset");
            }

            var combosInputActionMap = inputActionAsset.FindActionMap(COMBO_ACTION_MAP_NAME);
            if (combosInputActionMap == null)
            {
                Debug.Log("Creating 'Combos' input action map");
                combosInputActionMap = inputActionAsset.AddActionMap(COMBO_ACTION_MAP_NAME);
            }

            sequences.Each((sequence, idx) =>
            {
                using (var inputAction = combosInputActionMap.FindAction(sequence.name))
                {
                    if (inputAction == null)
                    {
                        Debug.Log("Creating input action " + sequence.name);

                        var newInputAction = combosInputActionMap.FindAction(sequence.name);
                        if (newInputAction == null)
                        {
                            newInputAction = combosInputActionMap.AddAction(sequence.name, InputActionType.Button);
                            if (newInputAction == null)
                            {
                                Debug.Log("failed to create input action " + sequence.name);
                                return;
                            }
                            newInputAction.AddBinding($"<SimulatedComboDevice>/{sequence.name}_comboButton{idx}");
                        }
                        generatedComboInputActionReferences[sequence.name] = InputActionReference.Create(newInputAction);
                    }
                    else
                    {
                        Debug.Log("Found input action " + sequence.name);
                        generatedComboInputActionReferences[sequence.name] = InputActionReference.Create(inputAction);
                    }
                }
            });
        }

        public void Initialize()
        {
            UpdateInputActionMap();
            InitializeTree();
        }

        public void InitializeTree()
        {
            sequenceTrees = new Dictionary<InputActionReference, NTree<InputActionReference>>();
            // Build a tree from the configured sequences
            foreach (var sequence in sequences)
            {
                uniqueInputActionReferences[sequence.inputActions[0]] = sequence.inputActions[0];
                sequenceTrees.TryGetValue(sequence.inputActions[0], out var sequenceTree);
                if (sequenceTree == null)
                {
                    Debug.Log("Creating new root node " + sequence.inputActions[0].name);
                    sequenceTree = new NTree<InputActionReference>(sequence.inputActions[0]);
                    sequenceTrees.Add(sequence.inputActions[0], sequenceTree);
                }
                else
                {
                    Debug.Log("Found existing root node " + sequence.inputActions[0]);
                }

                // loop through sequences and insert as children
                for (var i = 1; i < sequence.inputActions.Length; i++)
                {
                    uniqueInputActionReferences[sequence.inputActions[i]] = sequence.inputActions[i];
                    // loop through sequenceTree's children, if one matches the current input action reference, set it don't append again
                    bool skipAppend = false;
                    var j = 1;
                    do
                    {
                        var child = sequenceTree.GetChild(j);
                        if (child == null) { break; }

                        if (child.data == sequence.inputActions[i])
                        {
                            sequenceTree = child;
                            Debug.Log("Child already exists at this level" + sequenceTree.data.name + " == " + sequence.inputActions[i]);
                            skipAppend = true;
                            break;
                        }

                        j++;
                    } while (true);
                    if (!skipAppend)
                    {
                        Debug.Log("Appending child from " + sequenceTree.data.name + " to " + sequence.inputActions[i].name);
                        sequenceTree = sequenceTree.AddChild(sequence.inputActions[i]);
                    }
                }

                Debug.Log("Appending child from " + sequenceTree.data.name + " to " + generatedComboInputActionReferences[sequence.name].name);
                sequenceTree.AddChild(generatedComboInputActionReferences[sequence.name]);
            }
        }

        public Dictionary<InputActionReference, NTree<InputActionReference>> GetComboTrees()
        {
            if (sequenceTrees == null)
            {
                Initialize();
            }
            return sequenceTrees;
        }
        public InputActionReference[] GetUniqueInputActionReferences()
        {
            if (uniqueInputActionReferences.Count == 0)
            {
                Initialize();
            }
            return uniqueInputActionReferences.Values.ToArray();
        }

        public InputActionReference[] GetGeneratedComboInputActionReferences()
        {
            if (generatedComboInputActionReferences.Count == 0)
            {
                UpdateInputActionMap();
            }
            return generatedComboInputActionReferences.Values.ToArray();
        }

        public class NTree<T>
        {
            public T data;
            public LinkedList<NTree<T>> children;

            public NTree(T data)
            {
                this.data = data;
                children = new LinkedList<NTree<T>>();
            }

            public NTree<T> AddChild(T data)
            {
                var newTree = new NTree<T>(data);
                children.AddLast(newTree);
                return newTree;
            }

            public NTree<T> GetChild(int i)
            {
                foreach (NTree<T> n in children)
                    if (--i == 0)
                        return n;
                return null;
            }
        }
    }

    public static class SimulatedComboDeviceMessageBus
    {
        public delegate void ComboTriggerDelegate(InputActionReference inputActionReference);

        private static ComboTriggerDelegate comboTriggerDelegate;

        public static void TriggerCombo(InputActionReference inputActionReference)
        {
            comboTriggerDelegate(inputActionReference);
        }

        public static void Subscribe(ComboTriggerDelegate triggerDelegate)
        {
            comboTriggerDelegate -= triggerDelegate;
            comboTriggerDelegate += triggerDelegate;
        }
        public static void Unsubscribe(ComboTriggerDelegate triggerDelegate)
        {
            comboTriggerDelegate -= triggerDelegate;
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(ComboTree))]
    public class SimulatedComboDeviceScriptGenerator : Editor
    {

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ComboTree comboTreeData = (ComboTree)target;
            Dictionary<InputActionReference, int> comboInputActionIndex = new Dictionary<InputActionReference, int>();

            if (GUILayout.Button("Update Input Action Map"))
            {
                comboTreeData.UpdateInputActionMap();
            }

            if (GUILayout.Button("Generate Simulated Combo Device"))
            {
                comboTreeData.UpdateInputActionMap();
                StringBuilder inputControlAnnotations = new StringBuilder();
                StringBuilder buttonControlProperties = new StringBuilder();
                StringBuilder finishSetupButtonControlPropertyAssignment = new StringBuilder();

                comboTreeData.GetGeneratedComboInputActionReferences().Each((combo, idx) =>
                {
                    comboInputActionIndex[combo] = idx;
                    inputControlAnnotations.Append($@"
    [InputControl(name = ""{combo.action.name}_comboButton{idx}"", layout = ""Button"", bit = {idx})]");
                    buttonControlProperties.Append($@"
    public ButtonControl {combo.action.name}_comboButton{idx} {{ get; private set; }}");
                    finishSetupButtonControlPropertyAssignment.Append($@"
    {combo.action.name}_comboButton{idx} = GetChildControl<ButtonControl>(""{combo.action.name}_comboButton{idx}"");");
                });

                string template = $@"/*
*  This code was generated by SimulatedComboDeviceScriptGenerator
*/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using ComboTreeInputSystem;

public struct SimulatedComboDeviceState : IInputStateTypeInfo
{{
    public FourCC format => new FourCC('S', 'C', 'D', 'S');
    {inputControlAnnotations}
    public int combos;
}}
#if UNITY_EDITOR
[InitializeOnLoad] // Call static class constructor in editor.
#endif
[InputControlLayout(stateType = typeof(SimulatedComboDeviceState))]
public class SimulatedComboDevice : InputDevice, IInputUpdateCallbackReceiver
{{
    // [InitializeOnLoad] will ensure this gets called on every domain (re)load
    // in the editor.
    #if UNITY_EDITOR
    static SimulatedComboDevice()
    {{
        CreateDevice();
    }}

    #endif
    private static ComboTree comboTreeData;
    private static Dictionary<string, int> comboInputActionIndex = new Dictionary<string, int>();
    private static Queue<string> inputActionEvents = new Queue<string>();

    {buttonControlProperties}

    // In the player, [RuntimeInitializeOnLoadMethod] will make sure our
    // initialization code gets called during startup.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {{
        if (comboTreeData == null)
        {{
            var comboTreeGUID = AssetDatabase.FindAssets(""t:ComboTree"").First();
            var comboTreePath = AssetDatabase.GUIDToAssetPath(comboTreeGUID);
            comboTreeData = AssetDatabase.LoadAssetAtPath<ComboTree>(comboTreePath);
        }}
        var comboInputActionReferences = comboTreeData.GetGeneratedComboInputActionReferences();

        comboInputActionReferences.Each((combo, idx) =>
        {{
            Debug.Log(""adding control "" + combo.name + "" as comboButton"" + idx);
            comboInputActionIndex[combo.name] = idx;
        }});
        
        InputSystem.RegisterLayout<SimulatedComboDevice>(
            matches: new InputDeviceMatcher()
                .WithInterface(""Custom""));
    }}
    protected override void FinishSetup()
    {{
        base.FinishSetup();

        {finishSetupButtonControlPropertyAssignment}
    }}

    public static SimulatedComboDevice current {{ get; private set; }}

    public override void MakeCurrent()
    {{
        base.MakeCurrent();
        current = this;
        SimulatedComboDeviceMessageBus.Subscribe(this.queueCombo);
    }}

    // When one of our custom devices is removed, we want to make sure that if
    // it is the '.current' device, we null out '.current'.
    protected override void OnRemoved()
    {{
        base.OnRemoved();
        if (current == this)
        {{
            SimulatedComboDeviceMessageBus.Unsubscribe(this.queueCombo);
            current = null;
        }}
    }}

    public void queueCombo(InputActionReference inputActionReference)
    {{
        inputActionEvents.Enqueue(inputActionReference.name);
    }}

    public void OnUpdate()
    {{
        Dictionary<string, string> observedCombos = new Dictionary<string, string>();
        var state = new SimulatedComboDeviceState();
        while(inputActionEvents.TryDequeue(out var input))
        {{
            if (observedCombos.TryGetValue(input, out var combo))
            {{
                Debug.Log($""already observed combo: {{combo}} "");
                continue;
            }} else
            {{
                observedCombos[input] = input;
            }}
            if (comboInputActionIndex.TryGetValue(input, out var index))
            {{
                state.combos |= 1 << index;
            }}
        }}

        InputSystem.QueueStateEvent(this, state);
    }}

#if UNITY_EDITOR
    [MenuItem(""Tools/Combo Input System/Create Device"")]
    public static void CreateDevice()
    {{
        Initialize();
        // This is the code that you would normally run at the point where
        // you discover devices of your custom type.
        if (InputSystem.devices.FirstOrDefault(x => x is SimulatedComboDevice) == null)
        {{
            InputSystem.AddDevice<SimulatedComboDevice>();
        }}
    }}

    // For completeness sake, let's also add code to remove one instance of our
    // custom device. Note that you can also manually remove the device from
    // the input debugger by right-clicking in and selecting ""Remove Device"".
    [MenuItem(""Tools/Combo Input System/Remove Device"")]
    private static void RemoveDevice()
    {{
        var customDevice = InputSystem.devices.FirstOrDefault(x => x is SimulatedComboDevice);
        if (customDevice != null)
            InputSystem.RemoveDevice(customDevice);
    }}

#endif

}}
";
                System.IO.File.WriteAllText($"{Application.dataPath}/SimulatedComboDevice.cs", template);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Combo Tree", "Generated Simluated Combo Device " + comboTreeData.name, "OK", "");
            }
        }
    }
#endif
}
