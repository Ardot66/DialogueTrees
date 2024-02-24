# if TOOLS

using Godot;
using Ardot.DialogueTrees.DialogueNodes;

namespace Ardot.DialogueTrees.DialogueVariables;

///<summary>Handles the UI for defining certain aspects </summary>
[Tool]
public abstract partial class DialogueVariableDefinition : VBoxContainer
{
	public DialogueVariableNode VariableNode;

	public abstract Variant GetDefinition();
	
	public abstract void SetDefinition(Variant definition);
}

# endif