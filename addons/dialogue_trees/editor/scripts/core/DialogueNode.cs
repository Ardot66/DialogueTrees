# if TOOLS

using Godot;
using Godot.Collections;
using Ardot.DialogueTrees.Runtime;

namespace Ardot.DialogueTrees.Editor;

[Tool]
public abstract partial class DialogueNode : GraphNode
{
	private DialogueNodeData _nodeData;
	public DialogueNodeData NodeData {get => _nodeData;}

	private DialogueGraph _dialogueGraph;
	public DialogueGraph DialogueGraph {get => _dialogueGraph;}

	public abstract DialogueNodeSaveData Save();
	public abstract void Load(DialogueNodeSaveData data);

	public void Setup(DialogueNodeData nodeData, DialogueGraph dialogueGraph)
	{
		if(NodeData != null)
			return;

		_nodeData = nodeData;
		_dialogueGraph = dialogueGraph;
		TooltipText = nodeData.DialogueNodeTooltip;
	}

	///<summary>Gets the <c>EditorUndoRedoManager</c> that this plugin is using.</summary>
	public EditorUndoRedoManager GetUndoRedo()
	{
		return DialogueGraph.UndoRedo;
	}

	///<summary>Gets the current edited DialogueTree. This may be null if no tree is being edited.</summary>
	public DialogueTree GetDialogueTree()
	{
		return DialogueGraph.Dock.EditedDialogueTree;
	}
}
# endif
