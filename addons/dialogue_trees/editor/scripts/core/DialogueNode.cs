# if TOOLS

using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees;

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

public readonly struct DialogueNodeSaveData
{
	/// <summary>
	/// Creates a new DialogueNodeSaveData. Any parameter may be set to null to save memory on empty arrays.
	/// </summary>
	/// <param name="generalData"></param>
	/// <param name="nodeReferences"></param>
	public DialogueNodeSaveData(Array generalData, Array<int> nodeReferences)
	{
		General = generalData;
		References = nodeReferences;
	}

	/// <summary>
	/// General data section for storing data. Not to be used for storing IDs that reference other DialogueNodes.
	/// </summary>
	public readonly Array General;

	/// <summary>
	/// Data section specifically for storing reference IDs to other DialogueNodes. IDs must be placed here to be automatically updated.<br/>
	/// Any references in this list during a load may not point to a valid node, so always check if references are valid.
	/// </summary>
	public readonly Array<int> References;
}

# endif
