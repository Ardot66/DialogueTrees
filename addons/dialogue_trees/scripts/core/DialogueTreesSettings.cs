using Godot;
using System;
using System.Linq;

namespace Ardot.DialogueTrees;

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
	DialogueTreesSettingsPath = $"{DialogueTreesPlugin.DialogueTreesPluginPath}/dialogue_trees_settings.tres";

	public static DialogueTreesSettings Singleton => _singleton;

	private static DialogueTreesSettings _singleton = null;

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

	///<summary>Helper function for adding <c>DialogueNodeData</c> resources to the DialogueNodeData list.</summary>
	public void AddDialogueNodeData(DialogueNodeData[] dialogueNodeData)
	{
		DialogueNodeData = _dialogueNodeData.Concat(dialogueNodeData).ToArray();
	}

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

	public static DialogueTreesSettings LoadSettings()
	{
		if(!ResourceLoader.Exists(DialogueTreesSettingsPath))
		{
			GD.PrintErr($"The DialogueTreeSettings is missing, please reinstall the DialogueTrees plugin or place a new DialogueTreesSettings at {DialogueTreesSettingsPath} (If you place a new one, the default dialogue nodes will not be included unless you add them manually)");
			return null;
		}

		return ResourceLoader.Load<DialogueTreesSettings>(DialogueTreesSettingsPath);
	}
}
