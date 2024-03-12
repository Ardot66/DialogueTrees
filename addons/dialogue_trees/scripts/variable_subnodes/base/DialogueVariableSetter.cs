# if TOOLS

using Ardot.DialogueTrees.DialogueNodes;
using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees.DialogueVariables;

[Tool]
public abstract partial class DialogueVariableSetter : HBoxContainer
{
	public DialogueVariableNode VariableNode;

	public abstract Array Save();

	public abstract void Load(Array data);
}

# endif