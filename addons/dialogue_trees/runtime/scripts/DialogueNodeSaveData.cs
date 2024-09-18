using Godot.Collections;

namespace Ardot.DialogueTrees.Runtime;

public readonly struct DialogueNodeSaveData
{
	/// <summary>
	/// Creates a new DialogueNodeSaveData. Any parameter may be set to null to save memory on empty arrays.
	/// </summary>
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
	/// Use this section to save and load connections.
	/// </summary>
	public readonly Array<int> References;
}