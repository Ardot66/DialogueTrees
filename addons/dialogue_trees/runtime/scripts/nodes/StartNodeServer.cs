using Godot;
using System;

namespace Ardot.DialogueTrees.Runtime;

public partial class StartNodeServer : DialogueNodeServer
{
    public override bool RecieveInput(DialogueTree dialogueTree, int index, DialogueNodeSaveData saveData, Variant inputData)
    {
		if(saveData.References.Count <= 0)
			return true;

		int output = saveData.References[0];
		dialogueTree.PushToOutputStack(new DialogueNodeOutputData(dialogueTree, output, default));

		return true;
    }
}
