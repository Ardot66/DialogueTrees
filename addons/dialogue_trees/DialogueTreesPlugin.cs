#if TOOLS

using Godot;
using System;

namespace Ardot.DialogueTrees;

[Tool]
public partial class DialogueTreesPlugin : EditorPlugin
{
	public const string 
	DialogueTreesPluginPath = "res://addons/dialogue_trees";

	private const string
	_dialogueTreeDockScenePath = $"{DialogueTreesPluginPath}/scenes/editor/dialogue_tree_dock.tscn";

	public EditorUndoRedoManager UndoRedo;
	public DialogueTreeDock Dock;
	public Button BottomPanelButton;

	public DialogueTree EditedDialogueTree;

    public override void _EnterTree()
	{
		UndoRedo = GetUndoRedo();

		DialogueTreeSettings.LoadSettings();
		Dock = ResourceLoader.Load<PackedScene>(_dialogueTreeDockScenePath).Instantiate<DialogueTreeDock>();

		BottomPanelButton = AddControlToBottomPanel(Dock, "Dialogue");
		_MakeVisible(false);

		Dock.PluginReady(this);
	}

	public override void _ExitTree()
	{
		RemoveControlFromBottomPanel(Dock);
		Dock.QueueFree();
	}

	public override bool _Handles(GodotObject @object)
	{
		return @object is DialogueTree;
	}

	public override void _MakeVisible(bool visible)
	{
		if(!visible)
			HideBottomPanel();

		BottomPanelButton.Visible = visible;
		Dock.DockVisible = visible;
	}

    public override void _ApplyChanges()
    {
        if(EditedDialogueTree != null)
			Dock.SaveTree();
    }

    public override void _Edit(GodotObject @object)
    {
		if(@object is DialogueTree dialogueTree && IsInstanceValid(@object))
		{
			if(EditedDialogueTree != dialogueTree)
			{
				Dock.LoadTree(dialogueTree);
				
				EditedDialogueTree = dialogueTree;
			}

			MakeBottomPanelItemVisible(Dock);
		}
		else if (@object == null || !IsInstanceValid(@object))
			Dock.SaveTree();
    }
}
#endif
