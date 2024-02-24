# if TOOLS

using Godot;
using Ardot.DialogueTrees.DialogueNodes;

namespace Ardot.DialogueTrees.DialogueVariables;

///<summary>Handles the UI for setting the default value of a dialogue variable of a certain type.</summary>
[Tool]
public abstract partial class DialogueVariableDefaultValueSetter : VBoxContainer
{
	///<summary>The <c>DialogueVariableNode</c> that created this <c>DialogueVariableDefaultValueSetterNode</c>.</summary>
	public DialogueVariableNode VariableNode;

	///<summary>Returns the selected default value of this node.</summary>
	public abstract Variant GetValue();

	///<summary>Called on load to set the selected default value of this node.</summary>
	public abstract void SetValue(Variant variant);
}

# endif