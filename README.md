# Dialogue Trees C#
Dialogue Trees is a plugin that adds a graph-based dialogue editor.

### Please Note:
Dialogue Trees is still in early development, this means that you will likely encounter bugs and issues, and that compatiblity breaking changes will happen in the future.

# Getting Started
To begin with, you need to download the assets/dialogue_trees folder and add it to your project. After that, build your C# project and activate the plugin.

To start making dialogue, open up a scene and add a DialogueTree node. Click on the node, then in the inspector click 'Tree Data' and create a new TreeData.

Now, you should see an option in the bottom tab that says 'Dialogue' (the bottom tab is where you usually see Output and Debugger). When you click 'Dialogue' you will see the dialogue editor open.

In the editor, you can finally create nodes by right clicking, or by clicking "Add Node".

## Dialogue Nodes
There are currently 10 dialogue nodes by default, this is a list of all of them and a basic description of each.
| Node | Description |
| --- | --- |
| Start | The start node is activated when DialogueTree.StartDialogue() is called. There can only be one start node, and it cannot be deleted. |
| Switch | Awaits input from the player, and compares that input to a list of options. Options support regex, which can be useful if you are using a system where the player writes a response. For more fixed systems, you can get the list of options by calling DialogueTree.GetInputOptions(). |
| Output | Outputs text and a character's name by sending the DialogueTree.DialogueOutput signal. |
| Call | Activates a selected Function node. Mainly exists as a tool to make dialogue trees more readable. |
| Function | Acts as a reciever for a Call node. |
| Action | Activates a selected DialogueAction, DialogueActions are nodes that need to be added as children of a dialogue tree, and allow a dialogue tree to interact with the scene tree. DialogueActions are also designed to be very easy to extend, so you can create your own for certain situations. |
| Condition | Checks if a selected DialogueCondition is true, DialogueConditions are nodes that need to be added as children of a dialogue tree, and allow a dialogue tree to be affected by the state of the scene tree. Dialogue Conditions are designed to be very easy to extend, so you can create your own for certain situations. |
| Variable | Defines a variable that can be modified and compared by Variable Setters and Variable Conditions. |
| Variable Setter | Modifies the value of a varaible. |
| Variable Condition | Checks if a variable passes a certain condition. |

## Using the DialogueTree API
On its own, a DialogueTree will do nothing. It needs to be activated externally, and its inputs and outputs need to be handled via the API.

There are a few functions you will need to know, here's a list of them:
| Function | Description | 
| --- | --- |
| void StartDialogue () | Starts the dialogue by activating the Start node. |
| void EndDialogue () | Prematurely ends the dialogue. |
| void SendInput (string input, params Variant[] parameters) | Sends an input string to the active node. Parameters is not used by default. Some nodes, like output nodes, may require an empty input to signal that they can continue the dialogue. |
| DialogueInputOption[] GetInputOptions () | Gets a list of options that can be passed into SendInput (). The Parameters field of DialogueInputOption is not used by default. Some nodes, like output nodes, that need an empty input to continue will include that empty input in this list of options. |

There are also some signals that you will need to know, here's a list of them:
| Signal | Parameters | Description |
| --- | --- | --- |
| DialogueEnded | | Called when the dialogue ends, usually because a node had nowhere to output to, but can also be called by EndDialogue (). |
| DialogueOutput | string output, string character, Array parameters | Called when the dialogue tree outputs text, by default this is only called by Output nodes, and parameters is not used. By default, this also signals that an empty input is required to continue the dialogue. |

### Example
Here is a basic script that executes dialogue trees by printing outputs to the console and by choosing the first possible input every time.

``` C#
using Ardot.DialogueTrees;
using Godot;

public partial class DialogueReader : Node
{
	[Export]
	public DialogueTree DialogueTree;

	public override void _Ready()
	{
		DialogueTree.DialogueOutput += (string dialogue, string character, Godot.Collections.Array parameters) => GD.Print($"{character}: {dialogue}");
		DialogueTree.StartDialogue ();

		while (DialogueTree.DialogueActive)
		{
			DialogueInputOption inputOption = DialogueTree.GetInputOptions()[0];
			DialogueTree.SendInput(inputOption.Input, inputOption.Parameters);
		}
	}
}
```


