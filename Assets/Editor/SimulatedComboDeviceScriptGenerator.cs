﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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

            string template = $@"using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

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
        // Trigger our RegisterLayout code in the editor.
        Initialize();
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
        Debug.Log(comboInputActionReferences);

        comboTreeData.GetGeneratedComboInputActionReferences().Each((combo, idx) =>
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

    public static void QueueCombo(InputActionReference inputActionReference)
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
            InputSystem.AddDevice<SimulatedComboDevice>();
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
            Debug.Log(template);
            System.IO.File.WriteAllText($"{Application.dataPath}/SimulatedComboDevice.cs", template);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Combo Tree", "Generated Simluated Combo Device " + comboTreeData.name, "OK", "");
        }
    }
}