using Godot;
using Godot.Collections;
using Ardot.DialogueTrees.DialogueNodes;

namespace Ardot.DialogueTrees.DialogueVariables;

public abstract partial class DialogueVariableCondition : HBoxContainer
{
	public DialogueVariableNode VariableNode;

	public abstract Array Save();

	public abstract void Load(Array data);
}
