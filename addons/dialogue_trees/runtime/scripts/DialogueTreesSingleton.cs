using Godot;
using System;
using System.Collections.Generic;

namespace Ardot.DialogueTrees.Runtime;

public partial class DialogueTreesSingleton : Node
{
	private readonly Dictionary<StringName, DialogueNodeSingleton> _singletons = new();

    public override void _Ready()
    {
		DialogueTreesSettings settings = DialogueTreesSettings.Singleton;

		foreach(DialogueNodeData dialogueNodeData in settings.DialogueNodeData)
		{
			Node dialogueNodeSingletonNode = dialogueNodeData.DialogueNodeSingletonScene.Instantiate();

			if(dialogueNodeSingletonNode is not DialogueNodeSingleton dialogueNodeSingleton)
				continue;

			_singletons.Add(dialogueNodeData.DialogueNodeSaveName, dialogueNodeSingleton);
			AddChild(dialogueNodeSingleton);
		}
    }

	public DialogueNodeSingleton GetNodeSingleton(StringName nodeID)
	{
		return _singletons[nodeID];
	}
}
