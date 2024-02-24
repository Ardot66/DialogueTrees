using Godot;
using System;

namespace Ardot.DialogueTrees;

[Tool]
[GlobalClass]
public partial class DialogueTreeSettings : Resource
{
	public DialogueTreeSettings()
	{
		_singleton = this;
	}

	public const string 
	DialogueTreeSettingsPath = $"{DialogueTreesPlugin.DialogueTreesPluginPath}/dialogue_tree_settings.tres";

	public static DialogueTreeSettings Singleton => _singleton;

	private static DialogueTreeSettings _singleton = null;

	///<summary>Called when the <c>DialogueNodeData</c> variable of this <c>DialogueTreeSettings</c> is modified.</summary>
	[Signal]
	public delegate void DialogueNodeCollectionChangedEventHandler();

	private DialogueNodeData[] _dialogueNodeData = Array.Empty<DialogueNodeData>();

	[Export]
	public DialogueNodeData[] DialogueNodeData 
	{
		get => _dialogueNodeData;
		
		set
		{
			_dialogueNodeData = value;

			foreach(DialogueNodeData nodeData in _dialogueNodeData)
				if(nodeData != null)
					nodeData.dialogueNodeCollection = this;

			EmitSignal(SignalName.DialogueNodeCollectionChanged);
		} 
	}

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

	public void NotifyCollectionModified()
	{
		EmitSignal(SignalName.DialogueNodeCollectionChanged);
	}

	public static DialogueTreeSettings LoadSettings()
	{
		if(!ResourceLoader.Exists(DialogueTreeSettingsPath))
		{
			GD.PrintErr($"The DialogueTreeSettings is missing, please reinstall the plugin or place a new DialogueTreeSettings at {DialogueTreeSettingsPath} (If you place a new one, the default dialogue nodes will not be included unless you add them manually)");
			return null;
		}

		return ResourceLoader.Load<DialogueTreeSettings>(DialogueTreeSettingsPath);
	}
}
