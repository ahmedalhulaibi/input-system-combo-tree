using System;
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
{
    public FourCC format => new FourCC('S', 'C', 'D', 'S');
    
[InputControl(name = "special_1_comboButton0", layout = "Button", bit = 0)]
[InputControl(name = "special_2_comboButton1", layout = "Button", bit = 1)]
[InputControl(name = "special_3_comboButton2", layout = "Button", bit = 2)]
    public int combos;
}
#if UNITY_EDITOR
[InitializeOnLoad] // Call static class constructor in editor.
#endif
[InputControlLayout(stateType = typeof(SimulatedComboDeviceState))]
public class SimulatedComboDevice : InputDevice, IInputUpdateCallbackReceiver
{
    // [InitializeOnLoad] will ensure this gets called on every domain (re)load
    // in the editor.
    #if UNITY_EDITOR
    static SimulatedComboDevice()
    {
        // Trigger our RegisterLayout code in the editor.
        Initialize();
    }

    #endif
    private static ComboTree comboTreeData;
    private static Dictionary<string, int> comboInputActionIndex = new Dictionary<string, int>();
    private static Queue<string> inputActionEvents = new Queue<string>();

    
public ButtonControl special_1_comboButton0 { get; private set; }
public ButtonControl special_2_comboButton1 { get; private set; }
public ButtonControl special_3_comboButton2 { get; private set; }

    // In the player, [RuntimeInitializeOnLoadMethod] will make sure our
    // initialization code gets called during startup.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        if (comboTreeData == null)
        {
            var comboTreeGUID = AssetDatabase.FindAssets("t:ComboTree").First();
            var comboTreePath = AssetDatabase.GUIDToAssetPath(comboTreeGUID);
            comboTreeData = AssetDatabase.LoadAssetAtPath<ComboTree>(comboTreePath);
        }
        var comboInputActionReferences = comboTreeData.GetGeneratedComboInputActionReferences();
        Debug.Log(comboInputActionReferences);

        comboTreeData.GetGeneratedComboInputActionReferences().Each((combo, idx) =>
        {
            Debug.Log("adding control " + combo.name + " as comboButton" + idx);
            comboInputActionIndex[combo.name] = idx;
        });
        
        InputSystem.RegisterLayout<SimulatedComboDevice>(
            matches: new InputDeviceMatcher()
                .WithInterface("Custom"));
    }
    protected override void FinishSetup()
    {
        base.FinishSetup();

        
special_1_comboButton0 = GetChildControl<ButtonControl>("special_1_comboButton0");
special_2_comboButton1 = GetChildControl<ButtonControl>("special_2_comboButton1");
special_3_comboButton2 = GetChildControl<ButtonControl>("special_3_comboButton2");
    }

    public static void QueueCombo(InputActionReference inputActionReference)
    {
        inputActionEvents.Enqueue(inputActionReference.name);
    }

    public void OnUpdate()
    {
        Dictionary<string, string> observedCombos = new Dictionary<string, string>();
        var state = new SimulatedComboDeviceState();
        while(inputActionEvents.TryDequeue(out var input))
        {
            if (observedCombos.TryGetValue(input, out var combo))
            {
                Debug.Log($"already observed combo: {combo} ");
                continue;
            } else
            {
                observedCombos[input] = input;
            }
            if (comboInputActionIndex.TryGetValue(input, out var index))
            {
                state.combos |= 1 << index;
            }
        }

        InputSystem.QueueStateEvent(this, state);
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Combo Input System/Create Device")]
    public static void CreateDevice()
    {
            Initialize();
            // This is the code that you would normally run at the point where
            // you discover devices of your custom type.
            InputSystem.AddDevice<SimulatedComboDevice>();
    }

    // For completeness sake, let's also add code to remove one instance of our
    // custom device. Note that you can also manually remove the device from
    // the input debugger by right-clicking in and selecting "Remove Device".
    [MenuItem("Tools/Combo Input System/Remove Device")]
    private static void RemoveDevice()
    {
        var customDevice = InputSystem.devices.FirstOrDefault(x => x is SimulatedComboDevice);
        if (customDevice != null)
            InputSystem.RemoveDevice(customDevice);
    }

#endif

}
