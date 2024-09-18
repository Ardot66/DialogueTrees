using Godot;
using System;

namespace Ardot.DialogueTrees;

[Tool]
public partial class DialogueTreesPlugin 
# if TOOLS
: EditorPlugin
# endif
{
	# if TOOLS

	private const string _dialogueTreeDockScenePath = "res://addons/dialogue_trees/scenes/editor/dialogue_tree_dock.tscn";

	public EditorUndoRedoManager UndoRedo;
	public DialogueTreeDock Dock;
	public Button BottomPanelButton;

	public override void _EnterTree()
	{
		UndoRedo = GetUndoRedo();

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
	}

	public override void _ApplyChanges()
	{
		Dock.Save();
	}

	public override void _Edit(GodotObject @object)
	{
		if(@object is DialogueTree dialogueTree && IsInstanceValid(@object))
		{
			Dock.Edit(dialogueTree);
			MakeBottomPanelItemVisible(Dock);
		}
		else if (@object == null || !IsInstanceValid(@object))
			Dock.Edit(null);
	}
	
	#endif
}
