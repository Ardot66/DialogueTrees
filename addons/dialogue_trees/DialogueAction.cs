using Godot;
using Godot.Collections;
using System;

namespace Ardot.DialogueTrees.DialogueActions;

[Tool]
[GlobalClass]
public abstract partial class DialogueAction : Node
{
	///<summary>Invokes this <c>DialogueAction</c>.</summary>
	public abstract void Invoke(params Variant[] parameters);

	public DialogueTree GetDialogueTree()
	{
		return GetParentOrNull<DialogueTree>();
	}
}
