# Input System Combo Tree

Trigger an input action (through the input system) based on a sequence of inputs `Up, Up, Down, Down, Left, Right, Left, Right`

This repo demonstrates how to define custom input actions for combos/sequences of inputs.

## Getting started

### Prerequisites

You'll need to make sure to have the Input System package installed. Follow the [Unity guide](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/Installation.html) for installation instructions.

Note this was tested using Unity 2022.1.20f1 and Input System v1.4.4.

### Step 1 - Create an input action asset

This is based on your own project requirements. Follow the [Unity Quick Start Guide](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/QuickStartGuide.html) for help with this

### Step 2 - Add Combo Tree scripts to your project

Add these scripts to your project.
1. `ComboTree.cs`
1. `ComboTreeInput.cs`

### Step 3 - Create a Combo Tree asset

Create a `ComboTree` scriptable object. `Assets > Create > ScriptableObjects > ComboTree`

This is where you'll define the custom action names and sequences of input actions that are required to trigger said action.

Make sure to set the `Input Action Asset` field.

### Step 4 - Define your input sequences

Add your custom sequences.

In this project you'll find an example of this here [`Assets/ComboTrees.asset`](./Assets/ComboTrees.asset)

![](./readme_assets/combo_tree_example.png)

### Step 5 - Update the Input Action Asset

Press the `Update Input Action Map` button. This will create a new Input Action Map called `Combos` in your input action asset.

Any time you add a new sequence, make sure to press the `Update Input Action Map` button to keep your input actions up-to-date. 

### Step 6 - Generate Simulated Combo Device

Press the `Generate Simulated Combo Device` button. This will generate a C# file called `SimulatedComboDevice.cs` based on your sequences

Any time you add a new sequence, make sure to press the `Generate Simulated Combo Device` button to keep the simulated device up-to-date. 

### Step 7 - Add the input device to your project



## Known Issues and Limitations

The `Update Input Action Map` button will not remove stale/old input actions that no longer exist in the `ComboTree` asset. If you remove a sequence from your `ComboTree` asset you may have to remove it manually from the input action asset. Keeping it shouldn't affect any other sequences though.

The generated device only works for one player at the moment.

This is strictly an Input System extension and does not consider animations or animation state.