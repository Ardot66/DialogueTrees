# if TOOLS

using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ardot.DialogueTrees;

[Tool]
public partial class DialogueGraph : GraphEdit
{
	[Export]
	private PackedScene _arrangeNodeButtonScene;

	[Export]
	private PackedScene _deleteDialogueNodeButtonScene;

	[Export]
	private PackedScene _dialogueGraphAddNodeButtonScene;

	public const string 
	FROM_NODE = "from_node",
	FROM_PORT = "from_port",
	TO_NODE = "to_node",
	TO_PORT = "to_port";

	public EditorUndoRedoManager UndoRedo;
	public DialogueTreesPlugin Plugin;
	public DialogueTreeDock Dock;
	public DialogueTree DialogueTree;

	public DialogueTreeData TreeData => DialogueTree.TreeData;

	///<summary>The Position Offset that the next node added should have.</summary>
	public Vector2 AddNodePosition;

	private Vector2[] _childPositions;

	public bool ArrangingNodes;

	private Button _addNodeButton;
	private Button _arrangeSelectedNodesButton;
	private Button _arrangeAllNodesButton;

	private Godot.Collections.Dictionary<long, DialogueNode> _dialogueNodes = new ();
	public IReadOnlyDictionary<long, DialogueNode> DialogueNodes {get => _dialogueNodes;}

	///<summary>Called directly after a <c>DialogueNode</c> is removed from the graph manually by the user (not when removed by undo-redo). This allows for adding extra undo-redo instructions to the 'Delete Dialogue Nodes' action. <c>CommitAction()</c> is always automatically called with <c>true</c> as its parameter.<para/>
	///<b>Note:</b> Do not call <c>CreateAction()</c> or <c>CommitAction()</c> with <c>undoRedo</c>, as this will happen automatically.<para/>
	///<b>Note:</b> There is no non-undo-redo version of this signal, as you can just connect to <c>ChildExitingTree</c>.</summary>
	[Signal]
	public delegate void DialogueNodeRemovedUndoRedoEventHandler(DialogueNode removedNode, EditorUndoRedoManager undoRedo);

	public void PluginReady(DialogueTreesPlugin plugin)
	{
		Plugin = plugin;
		UndoRedo = Plugin.UndoRedo;
		Dock = Plugin.Dock;

		HBoxContainer graphMenu = GetMenuHBox();
		Node addNodeButtonContainer = _dialogueGraphAddNodeButtonScene.Instantiate();
		_addNodeButton = addNodeButtonContainer.GetNode<Button>("Button");
		Node arrangeNodesButtonContainer = _arrangeNodeButtonScene.Instantiate();
		_arrangeSelectedNodesButton = arrangeNodesButtonContainer.GetNode<Button>("ArrangeSelectedButton");
		_arrangeAllNodesButton = arrangeNodesButtonContainer.GetNode<Button>("ArrangeAllButton");

		graphMenu.AddChild(addNodeButtonContainer);
		graphMenu.MoveChild(addNodeButtonContainer, 0);

		graphMenu.AddChild(arrangeNodesButtonContainer);

		_arrangeSelectedNodesButton.Pressed += () => {
			OnNodeBeginMove();
			ArrangeGraph(true);
			OnNodeEndMove();
		};

		_arrangeAllNodesButton.Pressed += () => {
			OnNodeBeginMove();
			ArrangeGraph();
			OnNodeEndMove();
		};

		_addNodeButton.Pressed += OnAddNodeButtonPressed;
		BeginNodeMove += OnNodeBeginMove;
		EndNodeMove += OnNodeEndMove;
		DeleteNodesRequest += OnDeleteNodesRequest;
		ConnectionRequest += OnConnectionRequested;
		DisconnectionRequest += OnDisonnectionRequested;
		DialogueTree.TreeExiting += OnDialogueTreeExitingTree;
		DialogueTree.TreeDataChanged += OnDialogueTreeDataChanged;

		Visible = TreeData != null;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if(@event is InputEventMouse mouse && mouse.ButtonMask.HasFlag(MouseButtonMask.Right))
		{
			Vector2 mousePosition = GetLocalMousePosition();

			Dock.CreateNodePopup.Popup(new Rect2I(DisplayServer.MouseGetPosition(), Vector2I.Zero));
			AddNodePosition = (mousePosition + ScrollOffset) / Zoom;

			GetViewport().SetInputAsHandled();
		}
	}

	///<summary>Instantiates and sets up a Dialogue Node. May return null. You must manually call GraphReady() and Load() on the returned node.</summary>
	public DialogueNode InstantiateDialogueNode(DialogueNodeData nodeData, long? ID = null)
	{
		if(_dialogueNodes.Count >= nodeData.NodeLimit || !nodeData.TryInstantiateDialogueNode(out DialogueNode dialogueNode))
			return null;

		dialogueNode.Setup(nodeData, this, ID ?? Dock.DialogueNodeCount);

		if(!ID.HasValue)
			Dock.IncrementDialogueNodeCount();

		HBoxContainer titlebarHBox = dialogueNode.GetTitlebarHBox();
		ValueButton deleteNodeButton = _deleteDialogueNodeButtonScene.Instantiate<ValueButton>();

		deleteNodeButton.Value = dialogueNode;
		deleteNodeButton.ValueButtonPressed += OnDeleteNodeButtonPressed;
		titlebarHBox.AddChild(deleteNodeButton);

		return dialogueNode;
	}

	public void DisconnectNode(Dictionary connection)
	{
		DisconnectNode(connection[FROM_NODE].AsStringName(), connection[FROM_PORT].AsInt32(), connection[TO_NODE].AsStringName(), connection[TO_PORT].AsInt32());
	}

	public void ConnectNode(Dictionary connection)
	{
		ConnectNode(connection[FROM_NODE].AsStringName(), connection[FROM_PORT].AsInt32(), connection[TO_NODE].AsStringName(), connection[TO_PORT].AsInt32());
	}

	public void AddDialogueNode(DialogueNode node)
	{
		_dialogueNodes.Add(node.ID, node);

		if(node != null)
			AddChild(node);
	}

	public void RemoveDialogueNode(DialogueNode node)
	{
		_dialogueNodes.Remove(node.ID);
		RemoveChild(node);
	}

	public void ClearTree()
	{
		if(TreeData == null)
			return;

		ClearConnections();

		foreach(DialogueNode node in _dialogueNodes.Values)
		{
			RemoveChild(node);
			node.QueueFree();
		}

		_dialogueNodes.Clear();
	}

	public void SelectAllNodes(bool selected = true)
	{
		foreach(DialogueNode node in _dialogueNodes.Values)
			node.Selected = selected;
	}

	public void ArrangeGraph(bool arrangeOnlySelected = false)
	{
		// Predicate<GraphNode> arrangeMatch = arrangeOnlySelected ? (node) => node.Selected : null;
		
		// GraphArranger3.ArrangeGraph(this, arrangeMatch, arrangeOnlySelected);

		GraphArranger graphArranger = new (arrangeOnlySelected, arrangeOnlySelected);
		
		graphArranger.ArrangeGraph(this);
	}

	public Array<Dictionary> GetConnectionsToPort(StringName node, int port, SlotType portType = SlotType.Any)
	{
		Array<Dictionary> connections = new();

		foreach(Dictionary con in GetConnectionList())
		{
			switch (portType)
			{
				case SlotType.Any:
					if((con[FROM_NODE].AsStringName() == node && con[FROM_PORT].AsInt32() == port) || (con[TO_NODE].AsStringName() == node && con[TO_PORT].AsInt32() == port))
						connections.Add(con);
					break;
				case SlotType.OutPort:
					if(con[FROM_NODE].AsStringName() == node && con[FROM_PORT].AsInt32() == port)
						connections.Add(con);
					break;
				case SlotType.InPort:
					if(con[TO_NODE].AsStringName() == node && con[TO_PORT].AsInt32() == port)
						connections.Add(con);
					break;
			}
		}
			
		return connections;
	}
 
	public Array<Dictionary> GetConnectionsToNode(StringName node, SlotType portType = SlotType.Any)
	{
		Array<Dictionary> connections = new();

		foreach(Dictionary con in GetConnectionList())
		{
			switch (portType)
			{
				case SlotType.Any:
					if(con[FROM_NODE].AsStringName() == node || con[TO_NODE].AsStringName() == node)
						connections.Add(con);
					break;
				case SlotType.OutPort:
					if(con[FROM_NODE].AsStringName() == node)
						connections.Add(con);
					break;
				case SlotType.InPort:
					if(con[TO_NODE].AsStringName() == node)
						connections.Add(con);
					break;
			}
		}

		return connections;
	}

	public Array<Dictionary> GetConnectionsToNodes(Array<StringName> nodes, SlotType portType = SlotType.Any)
	{
		Array<Dictionary> connections = new();

		foreach(Dictionary con in GetConnectionList())
		{	
			StringName
			fromNode = con[FROM_NODE].AsStringName(),
			toNode = con[TO_NODE].AsStringName();
		
			foreach(StringName node in nodes)
			{
				switch (portType)
				{
					case SlotType.Any:
						if(fromNode == node || toNode == node)
							connections.Add(con);
						break;
					case SlotType.OutPort:
						if(fromNode == node)
							connections.Add(con);
						break;
					case SlotType.InPort:
						if(toNode == node)
							connections.Add(con);
						break;
				}
			}
		}

		return connections;
	}

	public void SwitchPortConnections(StringName node, int oldPort, int newPort, SlotType portType = SlotType.Any)
	{
		foreach(Dictionary con in GetConnectionList())
		{
			switch (portType)
			{
				case SlotType.OutPort: CheckOutPort(); break;
				case SlotType.InPort: CheckInPort(); break;
				case SlotType.Any: CheckOutPort(); CheckInPort(); break;
			}

			void CheckOutPort()
			{
				if(con[FROM_PORT].AsInt32() == oldPort && con[FROM_NODE].AsStringName() == node)
				{
					DisconnectNode(con);
					ConnectNode(con[FROM_NODE].AsStringName(), newPort, con[TO_NODE].AsStringName(), con[TO_PORT].AsInt32());
				}
			}

			void CheckInPort()
			{
				if(con[TO_PORT].AsInt32() == oldPort && con[TO_NODE].AsStringName() == node)
				{
					DisconnectNode(con);
					ConnectNode(con[FROM_NODE].AsStringName(), con[FROM_PORT].AsInt32(), con[TO_NODE].AsStringName(), newPort);
				}
			}
		}
	}

	private void OnDeleteNodesRequest(Godot.Collections.Array nodes)
	{
		Array<StringName> nodeNames = new(nodes.Select(v => v.AsStringName()));

		UndoRedo.CreateAction("Delete Dialogue Nodes", Godot.UndoRedo.MergeMode.Disable, DialogueTree);

		foreach(StringName nodeName in nodeNames)
		{
			Node node = GetNode(nodeName.ToString());
			DialogueNode dialogueNode = node as DialogueNode;

			if(dialogueNode != null && !dialogueNode.NodeData.CanBeDeleted)
				continue;

			UndoRedo.AddDoMethod(this, MethodName.RemoveDialogueNode, dialogueNode);
			UndoRedo.AddUndoMethod(this, MethodName.AddDialogueNode, dialogueNode);
			UndoRedo.AddUndoReference(node);

			if(dialogueNode != null)
				EmitSignal(SignalName.DialogueNodeRemovedUndoRedo, dialogueNode, UndoRedo);
		}

		foreach(Dictionary con in GetConnectionsToNodes(nodeNames))
		{
			UndoRedo.AddDoMethod(this, MethodName.DisconnectNode, con);
			UndoRedo.AddUndoMethod(this, MethodName.ConnectNode, con);
		}

		UndoRedo.CommitAction();	
	} 

	private void OnNodeBeginMove()
	{
		int childCount = GetChildCount();

		_childPositions = new Vector2[childCount];

		for(int x = 0; x < childCount; x++)
		{
			GraphNode graphNode = GetChildOrNull<GraphNode>(x);

			if(graphNode == null)
				continue;

			_childPositions[x] = graphNode.PositionOffset;
		}
	}

	private void OnNodeEndMove()
	{
		if(ArrangingNodes)
		{
			ArrangingNodes = false;
			return;
		}

		int childCount = GetChildCount();

		UndoRedo.CreateAction("Move Dialogue Nodes", Godot.UndoRedo.MergeMode.Disable, DialogueTree);

		for(int x = 0; x < childCount; x++)
		{
			GraphNode graphNode = GetChildOrNull<GraphNode>(x);

			if(graphNode == null || graphNode.PositionOffset == _childPositions[x])
				continue;

			UndoRedo.AddDoProperty(graphNode, GraphElement.PropertyName.PositionOffset, graphNode.PositionOffset);
			UndoRedo.AddUndoProperty(graphNode, GraphElement.PropertyName.PositionOffset, _childPositions[x]);
		}

		UndoRedo.CommitAction(false);
	}

	private void OnConnectionRequested(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		UndoRedo.CreateAction("Add Connection", Godot.UndoRedo.MergeMode.Disable, DialogueTree);

		Array<Dictionary> existingConnections = GetConnectionsToPort(fromNode, (int)fromPort, SlotType.OutPort);

		for(int x = 0; x < existingConnections.Count; x++)
		{
			UndoRedo.AddDoMethod(this, MethodName.DisconnectNode, existingConnections[x]);
			UndoRedo.AddUndoMethod(this, MethodName.ConnectNode, existingConnections[x]);
		}

		UndoRedo.AddDoMethod(this, GraphEdit.MethodName.ConnectNode, fromNode, (int)fromPort, toNode, (int)toPort);
		UndoRedo.AddUndoMethod(this, GraphEdit.MethodName.DisconnectNode, fromNode, (int)fromPort, toNode, (int)toPort);
		
		UndoRedo.CommitAction();
	}

	private void OnDisonnectionRequested(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		UndoRedo.CreateAction("Remove Connection", Godot.UndoRedo.MergeMode.Disable, DialogueTree);
		UndoRedo.AddDoMethod(this, GraphEdit.MethodName.DisconnectNode, fromNode, (int)fromPort, toNode, (int)toPort);
		UndoRedo.AddUndoMethod(this, GraphEdit.MethodName.ConnectNode, fromNode, (int)fromPort, toNode, (int)toPort);
		UndoRedo.CommitAction();
	}

	private void OnDialogueTreeExitingTree()
	{
		Dock.DisposeGraph(this);
	}

	private void OnDialogueTreeDataChanged(DialogueTreeData newTreeData)
	{
		Visible = newTreeData != null;

		if(newTreeData == null)
			ClearTree();
		else
			newTreeData?.LoadTree(this, true);
	}

	private void OnAddNodeButtonPressed()
	{	
		Dock.CreateNodePopup.Popup(new Rect2I((Vector2I)_addNodeButton.GetScreenPosition() + new Vector2I (0, (int)_addNodeButton.Size.Y), Vector2I.Zero));

		AddNodePosition = (ScrollOffset + Size / 2) / Zoom;
	}

	private void OnDeleteNodeButtonPressed(Variant value)
	{
		OnDeleteNodesRequest(new Godot.Collections.Array() {((Node)value).Name});
	}

	[System.Flags]
	public enum SlotType
	{
		InPort = 1,
		OutPort = 2,
		Any = InPort | OutPort,
	}
}

# endif
