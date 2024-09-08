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
	/// Data section specifically for storing reference IDs to other DialogueNodes. IDs must be placed here to be automatically updated.
	/// </summary>
	public readonly Array<int> References;
}

# endif
