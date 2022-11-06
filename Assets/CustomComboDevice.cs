/*using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

public struct CustomComboDeviceState : IInputStateTypeInfo
{

    public FourCC format => new FourCC('S', 'C', 'M', 'B');
    public ulong comboState;
}
#if UNITY_EDITOR
[InitializeOnLoad] // Call static class constructor in editor.
#endif
[InputControlLayout(stateType = typeof(CustomComboDeviceState))]
public class CustomComboDevice : InputDevice, IInputUpdateCallbackReceiver
{

    private static ComboTree comboTreeData;
    private static Dictionary<InputActionReference, int> comboInputActionIndex = new Dictionary<InputActionReference, int>();
    // [InitializeOnLoad] will ensure this gets called on every domain (re)load
    // in the editor.
#if UNITY_EDITOR
    static CustomComboDevice()
    {
        // Trigger our RegisterLayout code in the editor.
        Initialize();
    }

#endif

    // In the player, [RuntimeInitializeOnLoadMethod] will make sure our
    // initialization code gets called during startup.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (comboTreeData == null)
        {
            var comboTreeGUID = AssetDatabase.FindAssets("t:ComboTree").First();
            var comboTreePath = AssetDatabase.GUIDToAssetPath(comboTreeGUID);
            comboTreeData = AssetDatabase.LoadAssetAtPath<ComboTree>(comboTreePath);
        }
        var comboInputActionReferences = comboTreeData.GetGeneratedComboInputActionReferences();


        var builder = new InputControlLayout.Builder()
    .WithName("Simulated Combo Device (Simulated Combo Device)")
    .WithFormat("SCMB");

        builder.AddControl("combos")
            .WithLayout("Combos")
            .WithBitOffset(0)
            .WithByteOffset(0)
            .WithSizeInBits((uint)comboInputActionReferences.Length);

        comboTreeData.GetGeneratedComboInputActionReferences().Each((combo, idx) =>
        {
            comboInputActionIndex[combo] = idx + 1;
            builder.AddControl(combo.name)
                .WithByteOffset(0)
                .WithBitOffset((uint)(idx + 1));
        });
        // Register our device with the input system. We also register
        // a "device matcher" here. These are used when a device is discovered
        // by the input system. Each device is described by an InputDeviceDescription
        // and an InputDeviceMatcher can be used to match specific properties of such
        // a description. See the documentation of InputDeviceMatcher for more
        // details.
        //
        // NOTE: In case your device is more dynamic in nature and cannot have a single
        //       static layout, there is also the possibility to build layouts on the fly.
        //       Check out the API documentation for InputSystem.onFindLayoutForDevice and
        //       for InputSystem.RegisterLayoutBuilder.
        InputSystem.RegisterLayoutBuilder(() => builder.Build(), "Simulated Combo Device");
        Debug.Log("registered custom combo device");
    }

    // FinishSetup is where our device setup is finalized. Here we can look up
    // the controls that have been created.
    protected override void FinishSetup()
    {
        base.FinishSetup();
    }

    // We can also expose a '.current' getter equivalent to 'Gamepad.current'.
    // Whenever our device receives input, MakeCurrent() is called. So we can
    // simply update a '.current' getter based on that.
    public static CustomComboDevice current { get; private set; }
    public override void MakeCurrent()
    {
        base.MakeCurrent();
        current = this;
    }

    // When one of our custom devices is removed, we want to make sure that if
    // it is the '.current' device, we null out '.current'.
    protected override void OnRemoved()
    {
        base.OnRemoved();
        if (current == this)
            current = null;
    }

    // So, this is all great and nice. But we have one problem. No one is actually
    // creating an instance of our device yet. Which means that while we can bind
    // to controls on the device from actions all we want, at runtime we will never
    // actually receive input from our custom device. For that to happen, we need
    // to make sure that an instance of the device is created at some point.
    //
    // This one's a bit tricky. Because it really depends on how the device is
    // actually discovered in practice. In most real-world scenarios, there will be
    // some external API that notifies us when a device under its domain is added or
    // removed. In response, we would report a device being added (using
    // InputSystem.AddDevice(new InputDeviceDescription { ... }) or removed
    // (using DeviceRemoveEvent).
    //
    // In this demonstration, we don't have an external API to query. And we don't
    // really have another criteria by which to determine when a device of our custom
    // type should be added.
    //
    // So, let's fake it here. First, to create the device, we simply add a menu entry
    // in the editor. Means that in the player, this device will never be functional
    // but this serves as a demonstration only anyway.
    //
    // NOTE: Nothing of the following is necessary if you have a device that is
    //       detected and sent input for by the Unity runtime itself, i.e. that is
    //       picked up from the underlying platform APIs by Unity itself. In this
    //       case, when your device is connected, Unity will automatically report an
    //       InputDeviceDescription and all you have to do is make sure that the
    //       InputDeviceMatcher you supply to RegisterLayout matches that description.
    //
    //       Also, IInputUpdateCallbackReceiver and any other manual queuing of input
    //       is unnecessary in that case as Unity will queue input for the device.

#if UNITY_EDITOR
    [MenuItem("Tools/Combo Input System/Create Device")]
    private static void CreateDevice()
    {
        Initialize();
        // This is the code that you would normally run at the point where
        // you discover devices of your custom type.
        InputSystem.AddDevice(new CustomComboDevice());
    }

    // For completeness sake, let's also add code to remove one instance of our
    // custom device. Note that you can also manually remove the device from
    // the input debugger by right-clicking in and selecting "Remove Device".
    [MenuItem("Tools/Combo Input System/Remove Device")]
    private static void RemoveDevice()
    {
        var customDevice = InputSystem.devices.FirstOrDefault(x => x is CustomComboDevice);
        if (customDevice != null)
            InputSystem.RemoveDevice(customDevice);
    }

#endif

    public void TriggerCombo(InputActionReference combo)
    {
        var state = new CustomComboDeviceState();
        state.comboState = (ulong)(1 << comboInputActionIndex[combo]);
        InputSystem.QueueStateEvent(this, state);
    }
    // So the other part we need is to actually feed input for the device. Notice
    // that we already have the IInputUpdateCallbackReceiver interface on our class.
    // What this does is to add an OnUpdate method that will automatically be called
    // by the input system whenever it updates (actually, it will be called *before*
    // it updates, i.e. from the same point that InputSystem.onBeforeUpdate triggers).
    //
    // Here, we can feed input to our devices.
    //
    // NOTE: We don't have to do this here. InputSystem.QueueEvent can be called from
    //       anywhere, including from threads. So if, for example, you have a background
    //       thread polling input from your device, that's where you can also queue
    //       its input events.
    //
    // Again, we don't have actual input to read here. So we just make up some stuff
    // here for the sake of demonstration. We just poll the keyboard
    //
    // NOTE: We poll the keyboard here as part of our OnUpdate. Remember, however,
    //       that we run our OnUpdate from onBeforeUpdate, i.e. from where keyboard
    //       input has not yet been processed. This means that our input will always
    //       be one frame late. Plus, because we are polling the keyboard state here
    //       on a frame-to-frame basis, we may miss inputs on the keyboard.
    //
    // NOTE: One thing we could instead is to actually use OnScreenControls that
    //       represent the controls of our device and then use that to generate
    //       input from actual human interaction.
    public void OnUpdate()
    {

        // Finally, queue the event.
        // NOTE: We are replacing the current device state wholesale here. An alternative
        //       would be to use QueueDeltaStateEvent to replace only select memory contents.
        
    }
}
*/