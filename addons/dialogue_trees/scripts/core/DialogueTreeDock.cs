# if TOOLS

using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace Ardot.DialogueTrees;

[Tool]
public partial class DialogueTreeDock : Control
{
	[Export]
	private PackedScene _dialogueGraphScene;

	[Export]
	private PopupMenu _dialogueGraphPopupMenu;

	[Export]
	private PopupMenu _dialogueGraphCreateNodeSubmenu;

	[Export]
	private InputEvent _openPopupInput;

	[Export]
	private string[] _fileDialogFilters = System.Array.Empty<string>();

	[Export]
	private float _fileDialogPopupSizeRatio = 0.4f;

	[Export]
	private Key key;

	[ExportGroup("Popup Item Data")]
	[Export]
	private int _createNodesItemID;

	[Export]
	private int _selectAllNodesItemID;

	[Export]
	private InputEventKey _selectAllNodesShortcut;

	[Export]
	private int _copySelectedItemID;

	[Export]
	private InputEventKey _copySelectedShortcut;

	[Export]
	private int _pasteItemID;

	[Export]
	private InputEventKey _pasteShortcut;

	[Export]
	private int _saveSelectedItemID;

	[Export]
	private int _loadFileItemID;

	public EditorUndoRedoManager UndoRedo;
	public DialogueTreesPlugin Plugin;

	private DialogueTreeData _editedDialogueTreeData;
	private DialogueTree _editedDialogueTree;
	public DialogueTree EditedDialogueTree {get => _editedDialogueTree;}

	private DialogueGraph _openDialogueGraph;
	private Godot.Collections.Dictionary<DialogueTreeData, DialogueGraph> _dialogueGraphs = new (); 

	private EditorFileDialog _dialogueGraphFileDialog;
	private DialogueTreeData _copiedTreeData;
	private Vector2 _dialogueGraphPopupPosition;

	public void PluginReady(DialogueTreesPlugin plugin)
	{
		Plugin = plugin;
		UndoRedo = plugin.UndoRedo;

		_dialogueGraphFileDialog = new ();
		AddChild(_dialogueGraphFileDialog);
		_dialogueGraphFileDialog.Filters = _fileDialogFilters;

		_dialogueGraphFileDialog.FileSelected += OnLoadTreeFileDialogueFileSelected;
		_dialogueGraphPopupMenu.IndexPressed += OnDialogueGraphPopupMenuIndexPressed;
		_dialogueGraphCreateNodeSubmenu.IndexPressed += OnCreateNodeSubmenuIndexPressed;

		_dialogueGraphPopupMenu.SetItemAccelerator(_dialogueGraphPopupMenu.GetItemIndex(_selectAllNodesItemID), _selectAllNodesShortcut.GetKeycodeWithModifiers());
		_dialogueGraphPopupMenu.SetItemAccelerator(_dialogueGraphPopupMenu.GetItemIndex(_copySelectedItemID), _copySelectedShortcut.GetKeycodeWithModifiers());
		_dialogueGraphPopupMenu.SetItemAccelerator(_dialogueGraphPopupMenu.GetItemIndex(_pasteItemID), _pasteShortcut.GetKeycodeWithModifiers());

		_dialogueGraphPopupMenu.SetItemSubmenuNode(_dialogueGraphPopupMenu.GetItemIndex(_createNodesItemID), _dialogueGraphCreateNodeSubmenu);

		int popupItemCount = 0;
		
		foreach(DialogueNodeData dialogueNodeData in DialogueTreesSettings.Singleton.DialogueNodeData)
		{
			if(dialogueNodeData == null || !dialogueNodeData.IncludeInAddNodeMenu || !dialogueNodeData.IsValid())
				continue;

			_dialogueGraphCreateNodeSubmenu.AddItem(dialogueNodeData.DialogueNodeName);
			_dialogueGraphCreateNodeSubmenu.SetItemMetadata(popupItemCount, dialogueNodeData);
			_dialogueGraphCreateNodeSubmenu.SetItemTooltip(popupItemCount, dialogueNodeData.DialogueNodeTooltip);

			popupItemCount++;
		}
	}

	public void PopupMenu(Rect2I? rect)
	{
		_dialogueGraphPopupMenu.Popup(rect);
		
		if(_openDialogueGraph != null)
			_dialogueGraphPopupPosition = _openDialogueGraph.Size / 2;
	}

    public override void _Input(InputEvent @event)
    {
		if(@event is not InputEventKey)
			return;

        if(_openDialogueGraph != null && _openDialogueGraph.HasFocus())
			if(_dialogueGraphPopupMenu.ActivateItemByEvent(@event))
				GetViewport().SetInputAsHandled();
    }

    public override void _GuiInput(InputEvent @event)
	{
		if(@event.IsMatch(_openPopupInput) && _editedDialogueTree != null)
		{
			Vector2 mousePosition = GetLocalMousePosition();

			_dialogueGraphPopupMenu.Popup(new Rect2I(DisplayServer.MouseGetPosition(), Vector2I.Zero));
			_dialogueGraphPopupPosition = mousePosition;

			GetViewport().SetInputAsHandled();
		}

		_dialogueGraphPopupMenu.ActivateItemByEvent(@event);
	}

	public void Save()
	{
	 	_openDialogueGraph?.SaveTree(_editedDialogueTree.TreeData);
	}	

	public void Edit(DialogueTree newDialogueTree)
	{
		if(newDialogueTree == _editedDialogueTree)
			return;

		if(_editedDialogueTree != null)
			_editedDialogueTree.TreeDataChanged -= ChangeTreeData;
		
		if(newDialogueTree != null)
		{
			newDialogueTree.TreeDataChanged += ChangeTreeData;
			ChangeTreeData(newDialogueTree.TreeData);
		}
		else
			ChangeTreeData(null);
			
		_editedDialogueTree = newDialogueTree;
	}

	private void ChangeTreeData(DialogueTreeData treeData)
	{
		if(_openDialogueGraph != null)
		{
			_openDialogueGraph.Visible = false;

			if(_editedDialogueTreeData != null)
				_openDialogueGraph.SaveTree(_editedDialogueTreeData);
		}

		if(treeData != null && treeData.IsValid())
		{
            DialogueGraph dialogueGraph;

            if (_dialogueGraphs.ContainsKey(treeData))
				dialogueGraph = _dialogueGraphs[treeData];	
			else
			{
				dialogueGraph = _dialogueGraphScene.Instantiate<DialogueGraph>();
				_dialogueGraphs.Add(treeData, dialogueGraph);

				AddChild(dialogueGraph);
				dialogueGraph.PluginReady(Plugin);
					
				Array<DialogueNode> dialogueNodes = dialogueGraph.PreloadTreeData(treeData, out Array<int> connections);	
				dialogueGraph.LoadTree(dialogueNodes, connections);
				
				dialogueGraph.CallDeferred(DialogueGraph.MethodName.ArrangeGraph, false);
			}

			dialogueGraph.Visible = true;
			_openDialogueGraph = dialogueGraph;
		}
		else	
		{
			_openDialogueGraph = null;

			_dialogueGraphPopupMenu.Hide();
			_dialogueGraphFileDialog.Hide();
		}

		_editedDialogueTreeData = treeData;
	}

	private void OnDialogueGraphPopupMenuIndexPressed(long index)
	{
		int itemID = _dialogueGraphPopupMenu.GetItemId((int)index);

		if(itemID == _loadFileItemID)
		{
			_dialogueGraphFileDialog.FileMode = EditorFileDialog.FileModeEnum.OpenFile;
			_dialogueGraphFileDialog.PopupCenteredRatio(_fileDialogPopupSizeRatio);
		}
		if(itemID == _saveSelectedItemID)
		{
			_dialogueGraphFileDialog.FileMode = EditorFileDialog.FileModeEnum.SaveFile;
			_dialogueGraphFileDialog.PopupCenteredRatio(_fileDialogPopupSizeRatio);
		}
		if(itemID == _selectAllNodesItemID && _openDialogueGraph != null)
			_openDialogueGraph.SelectAllNodes();
		if(itemID == _copySelectedItemID)
		{
			_copiedTreeData = new ();
			_openDialogueGraph?.SaveTree(_copiedTreeData, (DialogueNode node) => node.Selected);
		}
		if(itemID == _pasteItemID && _editedDialogueTree != null && _copiedTreeData != null && _copiedTreeData.IsValid() && _openDialogueGraph != null)
			LoadAdditionalDialogueTree(_openDialogueGraph, _copiedTreeData, "Paste Dialogue Tree", _editedDialogueTree);
	}

	private void OnCreateNodeSubmenuIndexPressed(long index)
	{
		if (_dialogueGraphCreateNodeSubmenu.GetItemMetadata((int)index).AsGodotObject() is not DialogueNodeData dialogueNodeData)
			return;

		DialogueNode dialogueNode = _openDialogueGraph.InstantiateDialogueNode(dialogueNodeData);

		if(dialogueNode == null)	
			return;

		UndoRedo.CreateAction("Add Dialogue Node", Godot.UndoRedo.MergeMode.Disable, _editedDialogueTree);
		UndoRedo.AddDoReference(dialogueNode);
		UndoRedo.AddDoMethod(_openDialogueGraph, DialogueGraph.MethodName.AddDialogueNode, dialogueNode);
		UndoRedo.AddDoProperty(dialogueNode, GraphElement.PropertyName.PositionOffset, _openDialogueGraph.ToGraphSpace(_dialogueGraphPopupPosition));
		UndoRedo.AddUndoMethod(_openDialogueGraph, DialogueGraph.MethodName.RemoveDialogueNode, dialogueNode);
		UndoRedo.CommitAction();
	}

	private void OnLoadTreeFileDialogueFileSelected(string path)
	{
		switch(_dialogueGraphFileDialog.FileMode)
		{
			case EditorFileDialog.FileModeEnum.SaveFile:
				DialogueTreeData treeData = ResourceLoader.Exists(path, typeof(DialogueTreeData).ToString()) ? ResourceLoader.Load<DialogueTreeData>(path) : new ();
				_openDialogueGraph.SaveTree(treeData, (DialogueNode node) => node.Selected);

				ResourceSaver.Save(treeData, path, ResourceSaver.SaverFlags.ChangePath);
				break;

			case EditorFileDialog.FileModeEnum.OpenFile:
				if(_openDialogueGraph == null || !ResourceLoader.Exists(path, typeof(DialogueTreeData).ToString()))
					return;

				DialogueTreeData loadedTreeData = ResourceLoader.Load<DialogueTreeData>(path);

				if(!loadedTreeData.IsValid())
					break;

				LoadAdditionalDialogueTree(_openDialogueGraph, loadedTreeData, "Load Dialogue Tree", _editedDialogueTree);
				break;
		}
	}

	private void LoadAdditionalDialogueTree(DialogueGraph dialogueGraph, DialogueTreeData dialogueTreeData, string undoRedoName, GodotObject undoRedoContext)
	{
		Array<DialogueNode> loadedNodes = dialogueGraph.PreloadTreeData(dialogueTreeData, out Array<int> connections);
		dialogueGraph.SelectAllNodes(false);

		UndoRedo.CreateAction(undoRedoName, Godot.UndoRedo.MergeMode.Disable, undoRedoContext);

		foreach(DialogueNode node in loadedNodes)
		{
			node.Selected = true;
			UndoRedo.AddDoReference(node);
		}

		UndoRedo.AddDoMethod(dialogueGraph, DialogueGraph.MethodName.LoadTree, loadedNodes, connections);
		UndoRedo.AddUndoMethod(dialogueGraph, DialogueGraph.MethodName.UnloadTree, loadedNodes, connections);
		UndoRedo.CommitAction();	

		dialogueGraph.CallDeferred(DialogueGraph.MethodName.ArrangeGraph, false);
	}
}

#endif
