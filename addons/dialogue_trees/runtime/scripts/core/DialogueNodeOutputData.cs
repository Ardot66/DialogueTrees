using Godot;
using System;

namespace Ardot.DialogueTrees.Runtime;

public readonly struct DialogueNodeOutputData
{
	public DialogueNodeOutputData (DialogueTree destinationDialogueTree, int destinationIndex, Variant data)
	{
		DestinationDialogueTree = destinationDialogueTree;
		DestinationIndex = destinationIndex;
		Data = data;
	}

	/// <summary>
	/// The data to be sent to the destination node.
	/// </summary>
	public readonly Variant Data;

	/// <summary>
	/// The DialogueTree that the destination node is located in.
	/// </summary>
	public readonly DialogueTree DestinationDialogueTree;

	/// <summary>
	/// The index of the destination node in the DestinationDialogueTree.
	/// </summary>
	public readonly int DestinationIndex;
}
