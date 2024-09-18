using Godot;
using System;
using System.Collections.Generic;

namespace Ardot.DialogueTrees.Runtime;

public partial class DialogueTreesServer : Node
{
	private readonly Dictionary<StringName, DialogueNodeServer> _dialogueNodeServers = new();

    public override void _Ready()
    {
		DialogueTreesSettings settings = DialogueTreesSettings.Singleton;

		foreach(DialogueNodeData dialogueNodeData in settings.DialogueNodeData)
		{
			Node dialogueNodeServerNode = dialogueNodeData.DialogueNodeServerScene.Instantiate();

			if(dialogueNodeServerNode is not DialogueNodeServer dialogueNodeServer)
				continue;

			_dialogueNodeServers.Add(dialogueNodeData.DialogueNodeSaveName, dialogueNodeServer);
			AddChild(dialogueNodeServer);
		}
    }

	public DialogueNodeServer GetNodeServer(StringName nodeID)
	{
		return _dialogueNodeServers[nodeID];
	}
}
