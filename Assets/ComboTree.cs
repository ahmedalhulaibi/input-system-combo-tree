using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;



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

        public void Traverse(NTree<T> node, Action<T> visitor)
        {
            visitor(node.data);
            foreach (NTree<T> kid in node.children)
                Traverse(kid, visitor);
        }

        public void Traverse(NTree<T> node, Action<NTree<T>> visitor)
        {
            visitor(node);
            foreach (NTree<T> kid in node.children)
                Traverse(kid, visitor);
        }

        public void Traverse(NTree<T> node, Action<NTree<T>, T> visitor)
        {
            visitor(node, node.data);
            foreach (NTree<T> kid in node.children)
                Traverse(kid, visitor);
        }
    }
}
