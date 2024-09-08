# if TOOLS

using Godot;
using Godot.Collections;
using System.Linq;

namespace Ardot.DialogueTrees;

[Tool]
public partial class DialogueTreeDock : Control
{
	[Export]
	private PackedScene _dialogueGraphScene;

	public DialogueTreesPlugin Plugin;
	public EditorUndoRedoManager UndoRedo;
	public DialogueGraph DialogueGraph;
	public Control ButtonContainer;
	public PopupMenu CreateNodePopup;
	public EditorFileDialog LoadTreeFileDialogue;
	public bool DockVisible;

	public bool DockFocused {get => GetRect().HasPoint(GetLocalMousePosition()) && DockVisible && Visible;}

	private long _dialogueNodeCount;
	public long DialogueNodeCount {get => _dialogueNodeCount;}

	public DialogueTreeData CopiedTreeData;

	public void PluginReady(DialogueTreesPlugin plugin)
	{
		Plugin = plugin;
		UndoRedo = plugin.UndoRedo;

		LoadTreeFileDialogue = new () {
			Filters = new string[] {"*.tres", "*.res"},
		};
		AddChild(LoadTreeFileDialogue);
		LoadTreeFileDialogue.FileSelected += OnLoadTreeFileDialogueFileSelected;

		CreateNodePopup = new();
		AddChild(CreateNodePopup);
		CreateNodePopup.IndexPressed += OnCreateNodePopupIndexPressed;

		LoadCreateNodePopup();
	}

	public void IncrementDialogueNodeCount()
	{
		_dialogueNodeCount++;
	}

	public void SaveTree()
	{
		DialogueGraph?.TreeData?.SaveTree(DialogueGraph);
	}	

	public void LoadTree(DialogueTree newDialogueTree)
	{
		if(DialogueGraph != null)
		{
			DialogueGraph.Visible = false;
			DialogueGraph?.TreeData?.SaveTree(DialogueGraph);
			DialogueGraph.Name = DialogueGraph.DialogueTree.GetInstanceId().ToString();
		}

		if(GetNodeOrNull(newDialogueTree.GetInstanceId().ToString()) is DialogueGraph cachedGraph)
		{
			cachedGraph.Visible = cachedGraph.TreeData != null;

			DialogueGraph = cachedGraph;
		}
		else
		{
			DialogueGraph newDialogueGraph = _dialogueGraphScene.Instantiate<DialogueGraph>();
			newDialogueGraph.DialogueTree = newDialogueTree;

			AddChild(newDialogueGraph);
			newDialogueGraph.PluginReady(Plugin);
				
			newDialogueGraph.TreeData?.LoadTree(newDialogueGraph, false);

			DialogueGraph = newDialogueGraph;
		}
	}

	public void DisposeGraph(DialogueGraph dialogueGraph)
	{	
		if(dialogueGraph == DialogueGraph)
		{
			DialogueGraph = null;
			Plugin.EditedDialogueTree = null;
		}
	
		RemoveChild(dialogueGraph);
		dialogueGraph.QueueFree();
	}	

	private void LoadCreateNodePopup()
	{
		if(CreateNodePopup == null)
			return;

		CreateNodePopup.Clear();

		int popupItemCount = 0;
		
		foreach(DialogueNodeData dialogueNodeData in DialogueTreesSettings.Singleton.DialogueNodeData)
		{
			if(dialogueNodeData == null || !dialogueNodeData.IncludeInAddNodeMenu)
				continue;

			CreateNodePopup.AddItem(dialogueNodeData.DialogueNodeName);
			CreateNodePopup.SetItemMetadata(popupItemCount, dialogueNodeData);

			if(!dialogueNodeData.IsValid())
			{
				CreateNodePopup.SetItemDisabled(popupItemCount, true);
				popupItemCount++;
				continue;
			}

			CreateNodePopup.SetItemTooltip(popupItemCount, dialogueNodeData.DialogueNodeTooltip);
			popupItemCount++;
		}

		CreateNodePopup.AddItem("");
		CreateNodePopup.SetItemAsSeparator(popupItemCount, true);
		popupItemCount++;

		string[] itemNames = new [] {"Select All", "Save Selected", "Load", "Copy Selected", "Paste"};
		string[] itemInternalNames = new [] {"SA", "SS", "L", "CS", "P"};
		string[] itemTooltips = new [] {"Selects all nodes in the tree.", "Saves all selected nodes to a file.", "Loads nodes from a file.", "Copies all selected nodes. (Ctrl C)", "Pastes any copied nodes."};
		
		for(int x = 0; x < itemNames.Length; x++, popupItemCount++)
		{
			CreateNodePopup.AddItem(itemNames[x]);
			CreateNodePopup.SetItemMetadata(popupItemCount, itemInternalNames[x]);
			CreateNodePopup.SetItemTooltip(popupItemCount, itemTooltips[x]);
		}
	}

	private void OnCreateNodePopupIndexPressed(long index)
	{
		Variant metadata = CreateNodePopup.GetItemMetadata((int)index);

		if(metadata.VariantType == Variant.Type.String)
		{
			string metadataName = metadata.AsString();

			switch(metadataName)
			{
				case "L":
					LoadTreeFileDialogue.FileMode = EditorFileDialog.FileModeEnum.OpenFile;
					LoadTreeFileDialogue.PopupCenteredRatio(0.6f);
					break;
				case "SA":
					DialogueGraph?.SelectAllNodes();
					break;
				case "SS":
					LoadTreeFileDialogue.FileMode = EditorFileDialog.FileModeEnum.SaveFile;
					LoadTreeFileDialogue.PopupCenteredRatio(0.6f);
					break;
				case "CS":
					CopiedTreeData = new ();
					CopiedTreeData.SaveTree(DialogueGraph, (DialogueNode node) => node.Selected);
					break;
				case "P":
					if(CopiedTreeData == null || DialogueGraph == null)
						break;

					Array<DialogueNode> loadedNodes = CopiedTreeData.LoadTree(DialogueGraph, false);

					UndoRedo.CreateAction("Paste Dialogue Tree", Godot.UndoRedo.MergeMode.Disable, DialogueGraph.DialogueTree);

					foreach(DialogueNode node in loadedNodes)
						UndoRedo.AddDoReference(node);

					UndoRedo.AddDoMethod(CopiedTreeData, DialogueTreeData.MethodName.LoadTree, DialogueGraph, false, false, loadedNodes);
					UndoRedo.AddUndoMethod(CopiedTreeData, DialogueTreeData.MethodName.UnloadTree, DialogueGraph, loadedNodes);
					UndoRedo.CommitAction(false);
					break;
			}

			return;
		}

		if (metadata.AsGodotObject() is DialogueNodeData dialogueNodeData)
		{
			DialogueNode dialogueNode = DialogueGraph.InstantiateDialogueNode(dialogueNodeData);

			if(dialogueNode == null)	
				return;

			UndoRedo.CreateAction("Add Dialogue Node", Godot.UndoRedo.MergeMode.Disable, DialogueGraph.DialogueTree);
			UndoRedo.AddDoReference(dialogueNode);
			UndoRedo.AddDoMethod(DialogueGraph, DialogueGraph.MethodName.AddDialogueNode, dialogueNode);
			UndoRedo.AddDoProperty(dialogueNode, GraphElement.PropertyName.PositionOffset, DialogueGraph.AddNodePosition);
			UndoRedo.AddUndoMethod(DialogueGraph, DialogueGraph.MethodName.RemoveDialogueNode, dialogueNode);
			UndoRedo.CommitAction();

			dialogueNode.GraphReady();
		}	
	}

	private void OnLoadTreeFileDialogueFileSelected(string path)
	{
		switch(LoadTreeFileDialogue.FileMode)
		{
			case EditorFileDialog.FileModeEnum.SaveFile:
				DialogueTreeData treeData = ResourceLoader.Exists(path, typeof(DialogueTreeData).ToString()) ? ResourceLoader.Load<DialogueTreeData>(path) : new ();
				treeData.SaveTree(DialogueGraph, (DialogueNode node) => node.Selected);

				ResourceSaver.Save(treeData, path, ResourceSaver.SaverFlags.ChangePath);
				break;

			case EditorFileDialog.FileModeEnum.OpenFile:
				if(DialogueGraph == null || !ResourceLoader.Exists(path, typeof(DialogueTreeData).ToString()))
					return;

				DialogueTreeData loadedTreeData = ResourceLoader.Load<DialogueTreeData>(path);
				Array<DialogueNode> loadedDialogueNodes = loadedTreeData.LoadTree(DialogueGraph, false);

				UndoRedo.CreateAction("Load Dialogue Tree", Godot.UndoRedo.MergeMode.Disable, DialogueGraph.DialogueTree);

				foreach(DialogueNode node in loadedDialogueNodes)
					UndoRedo.AddDoReference(node);

				UndoRedo.AddDoMethod(loadedTreeData, DialogueTreeData.MethodName.LoadTree, DialogueGraph, false, loadedDialogueNodes);
				UndoRedo.AddUndoMethod(loadedTreeData, DialogueTreeData.MethodName.UnloadTree, DialogueGraph, loadedDialogueNodes);
				UndoRedo.CommitAction(false);
				break;
		}
	}
}

#endif
