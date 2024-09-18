using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ardot.DialogueTrees.Runtime;

///<summary>Class for storing settings for dialogue nodes and dialogue trees. To add your own settings, create your own partial DialogueTreeSettings.</summary>

[Tool]
[GlobalClass]
public partial class DialogueTreesSettings : Resource
{
	public DialogueTreesSettings()
	{
		_singleton = this;
	}

	public const string 
	DialogueTreesSettingsPath = $"addons/dialogue_trees/dialogue_trees_settings.tres";

	private static DialogueTreesSettings _singleton = null;
	public static DialogueTreesSettings Singleton
	{
		get
		{
			if(_singleton == null)
			{
				if(!ResourceLoader.Exists(DialogueTreesSettingsPath))
				{
					GD.PrintErr($"The DialogueTreeSettings is missing, please reinstall the DialogueTrees plugin or place a new DialogueTreesSettings at {DialogueTreesSettingsPath} (If you place a new one, the default dialogue nodes will not be included unless you add them manually)");
					return null;
				}

				_singleton = ResourceLoader.Load<DialogueTreesSettings>(DialogueTreesSettingsPath);
			}

			return _singleton;
		}
	}

	[Export]
	private DialogueNodeData[] _dialogueNodeData = Array.Empty<DialogueNodeData>();
	public IReadOnlyList<DialogueNodeData> DialogueNodeData {get => _dialogueNodeData;}

	[Export]
	public DialogueTreeData DefaultTree = null;

	public DialogueNodeData GetDialogueNodeData(StringName dialogueNodeSaveName)
	{
		foreach(DialogueNodeData dialogueNodeData in DialogueNodeData)
		{
			if(dialogueNodeData.DialogueNodeSaveName == dialogueNodeSaveName)
				return dialogueNodeData;
		}

		return null;
	}
}
