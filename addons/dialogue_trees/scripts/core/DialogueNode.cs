# if TOOLS

using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees;

[Tool]
public abstract partial class DialogueNode : GraphNode
{
	public DialogueNodeData NodeData;
	public DialogueGraph DialogueGraph;

	public abstract Array Save();
	public abstract void Load(Array data);

	///<summary>Called after all <c>DialogueNode</c>s in the tree have been loaded.</summary>
	public virtual void GraphReady()
	{

	}

	///<summary>Gets the <c>EditorUndoRedoManager</c> that this plugin is using.</summary>
	public EditorUndoRedoManager GetUndoRedo()
	{
		return DialogueGraph.UndoRedo;
	}

	///<summary>Gets the <c>DialogueTree</c> that this <c>DialogueNode</c> is a part of.</summary>
	public DialogueTree GetDialogueTree()
	{
		return DialogueGraph.DialogueTree;
	}
}

# endif
