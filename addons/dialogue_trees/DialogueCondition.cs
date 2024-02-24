using Godot;
using Godot.Collections;
using System;

namespace Ardot.DialogueTrees.DialogueConditions;

[Tool]
[GlobalClass]
[Icon("res://addons/dialogue_trees/icons/dialogue_condition_icon.svg")]
public abstract partial class DialogueCondition : Node
{
	///<summary>Invokes this <c>DialogueCondition</c>.</summary>
	///<returns>Whether this condition is true.</returns>
	public abstract bool Invoke(params Variant[] parameters);

	public DialogueTree GetDialogueTree()
	{
		return GetParentOrNull<DialogueTree>();
	}
}
