# if TOOLS

using Godot;
using Godot.Collections;

using Ardot.DialogueTrees;

[Tool]
public partial class DialogueStartNode : DialogueNode
{
	public override DialogueNodeSaveData Save() => new (null, null);
	public override void Load(DialogueNodeSaveData data) {}
}

# endif
