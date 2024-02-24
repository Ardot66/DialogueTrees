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
	private const string 
	_arrangeNodesButtonScenePath = $"{DialogueTreesPlugin.DialogueTreesPluginPath}/scenes/editor/arrange_nodes_button.tscn",
	_deleteDialogueNodeButtonScenePath = $"{DialogueTreesPlugin.DialogueTreesPluginPath}/scenes/editor/delete_dialogue_node_button.tscn",
	_dialogueGraphAddNodeButtonScenePath = $"{DialogueTreesPlugin.DialogueTreesPluginPath}/scenes/editor/dialogue_graph_add_node_button.tscn",
	_fromNode = "from_node",
	_fromPort = "from_port",
	_toNode = "to_node",
	_toPort = "to_port";

	public EditorUndoRedoManager UndoRedo;
	public DialogueTreesPlugin Plugin;
	public DialogueTreeDock Dock;
	public DialogueTree DialogueTree;

	public DialogueTreeData TreeData => DialogueTree.TreeData;

	///<summary>The Position Offset that the next node added should have.</summary>
	public Vector2 AddNodePosition;

	private Vector2[] _childPositions;

	private bool _arrangingNodes;

	private Button _addNodeButton;
	private Button _arrangeSelectedNodesButton;
	private Button _arrangeAllNodesButton;

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
		Node addNodeButtonContainer = ResourceLoader.Load<PackedScene>(_dialogueGraphAddNodeButtonScenePath).Instantiate();
		_addNodeButton = addNodeButtonContainer.GetNode<Button>("Button");
		Node arrangeNodesButtonContainer = ResourceLoader.Load<PackedScene>(_arrangeNodesButtonScenePath).Instantiate();
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
	public DialogueNode InstantiateDialogueNode(DialogueNodeData nodeData)
	{
		if(GetChildCount(nodeData) >= nodeData.NodeLimit || !nodeData.TryInstantiateDialogueNode(out DialogueNode dialogueNode))
			return null;

		dialogueNode.NodeData = nodeData;
		dialogueNode.TooltipText = nodeData.DialogueNodeTooltip;
		dialogueNode.DialogueGraph = this;

		HBoxContainer titlebarHBox = dialogueNode.GetTitlebarHBox();
		ValueButton deleteNodeButton = ResourceLoader.Load<PackedScene>(_deleteDialogueNodeButtonScenePath).Instantiate<ValueButton>();

		deleteNodeButton.Value = dialogueNode;
		deleteNodeButton.ValueButtonPressed += OnDeleteNodeButtonPressed;
		titlebarHBox.AddChild(deleteNodeButton);

		return dialogueNode;
	}

	public void DisconnectNode(Dictionary connection)
	{
		DisconnectNode(connection[_fromNode].AsStringName(), connection[_fromPort].AsInt32(), connection[_toNode].AsStringName(), connection[_toPort].AsInt32());
	}

	public void ConnectNode(Dictionary connection)
	{
		ConnectNode(connection[_fromNode].AsStringName(), connection[_fromPort].AsInt32(), connection[_toNode].AsStringName(), connection[_toPort].AsInt32());
	}

	public void SaveTree(DialogueTreeData treeData, bool saveOnlySelected = false)
	{
		if(treeData == null)
			return;

		treeData.Clear();

		Array<DialogueNode> childrenToSave = new ();

		foreach(Node child in GetChildren())
		{
			if(child is not DialogueNode dialogueNode || (saveOnlySelected && !dialogueNode.Selected))
				continue;

			childrenToSave.Add(dialogueNode);
		}

		Array<StringName> nodeTypes = new ();

		foreach(DialogueNode dialogueNode in childrenToSave)
		{
			nodeTypes.Add(dialogueNode.NodeData.DialogueNodeSaveName);
			treeData.DialogueNodeSaveData.Add(dialogueNode.Save());
		}

		treeData.SetNodeTypes(nodeTypes);

		Array<Dictionary> connectionsList = GetConnectionList();
		List<DialogueTreeData.Connection> connections = new ();

		for(int x = 0; x < connectionsList.Count; x++)
		{
			Dictionary con = connectionsList[x];

			int
			fromNode = childrenToSave.IndexOf(GetNodeOrNull<DialogueNode>(con[_fromNode].AsStringName().ToString())),
			toNode = childrenToSave.IndexOf(GetNodeOrNull<DialogueNode>(con[_toNode].AsStringName().ToString()));

			if(fromNode != -1 && toNode != -1)
				connections.Add(new DialogueTreeData.Connection
				(
					fromNode,
					con[_fromPort].AsInt32(),
					toNode,
					con[_toPort].AsInt32()
				));
		}

		treeData.SetConnections(connections.ToArray());

		if(!string.IsNullOrEmpty(treeData.ResourcePath))
			ResourceSaver.Save(treeData, treeData.ResourcePath);
	}

	///<summary>Loads a <c>DialogueTreeData</c> into the graph. Allows for passing pre-instantiated <c>DialogueNode</c>s, this is mainly to allow for UndoRedo compatibility.</summary>
	public Array<Node> LoadTree(DialogueTreeData treeData, bool clearExistingTree = true, Array<Node> preLoadedNodes = null)
	{
		if(clearExistingTree)
			ClearTree();

		if(treeData == null)
			return null;

		Array<Node> loadedNodes = new ();
	
		if(preLoadedNodes == null)
			for (int x = 0; x < treeData.DialogueNodeSaveData.Count; x++)
			{
				Node node = InstantiateDialogueNode(DialogueTreeSettings.Singleton.GetDialogueNodeData(treeData.GetNodeType(x))) ?? new Node();
				AddDialogueNode(node, x);

				if(node is DialogueNode dialogueNode)
					dialogueNode.Load(treeData.DialogueNodeSaveData[x]);
			}
		else
			for(int x = 0; x < preLoadedNodes.Count; x++)
				AddDialogueNode(preLoadedNodes[x], x);

		void AddDialogueNode(Node node, int index)
		{
			AddChild(node);

			if(!clearExistingTree)
				MoveChild(node, index);

			loadedNodes.Add(node);
		}
			
		for(int x = 0; x < treeData.GetConnectionsCount(); x++)
		{
			DialogueTreeData.Connection con = treeData.GetConnection(x);

			Node fromNode = GetChild(con.FromNode);
			Node toNode = GetChild(con.ToNode);

			if(fromNode is not DialogueNode || toNode is not DialogueNode)
				continue;

			ConnectNode(fromNode.Name, con.FromPort, toNode.Name, con.ToPort);
		}

		SelectAllNodes(false);

		foreach(Node node in loadedNodes)
		{
			if(node is not DialogueNode dialogueNode)
				continue;

			dialogueNode.GraphReady();
			dialogueNode.Selected = true;
		}		

		_arrangingNodes = true;
		ArrangeGraph(true);

		return loadedNodes;
	}

	///<summary>Unloads the given DialogueTreeData and removes all loadedNodes, this is intended only for use with UndoRedo.</summary>
	public void UnloadTree(DialogueTreeData treeData, Array<Node> loadedNodes)
	{			
		for(int x = 0; x < treeData.GetConnectionsCount(); x++)
		{
			DialogueTreeData.Connection con = treeData.GetConnection(x);

			Node fromNode = GetChild(con.FromNode);
			Node toNode = GetChild(con.ToNode);

			if(fromNode is not DialogueNode || toNode is not DialogueNode)
				continue;

			DisconnectNode(fromNode.Name, con.FromPort, toNode.Name, con.ToPort);
		}

		for(int x = 0; x < loadedNodes.Count; x++)
			RemoveChild(loadedNodes[x]);
	}

	public void ClearTree()
	{
		if(TreeData == null)
			return;

		ClearConnections();

		foreach(Node node in GetChildren())
		{
			RemoveChild(node);
			node.QueueFree();
		}
	}

	public void SelectAllNodes(bool selected = true)
	{
		foreach(Node node in GetChildren())
		{
			if(node is not DialogueNode dialogueNode)
				continue;

			dialogueNode.Selected = selected;
		}
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
					if((con[_fromNode].AsStringName() == node && con[_fromPort].AsInt32() == port) || (con[_toNode].AsStringName() == node && con[_toPort].AsInt32() == port))
						connections.Add(con);
					break;
				case SlotType.OutPort:
					if(con[_fromNode].AsStringName() == node && con[_fromPort].AsInt32() == port)
						connections.Add(con);
					break;
				case SlotType.InPort:
					if(con[_toNode].AsStringName() == node && con[_toPort].AsInt32() == port)
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
					if(con[_fromNode].AsStringName() == node || con[_toNode].AsStringName() == node)
						connections.Add(con);
					break;
				case SlotType.OutPort:
					if(con[_fromNode].AsStringName() == node)
						connections.Add(con);
					break;
				case SlotType.InPort:
					if(con[_toNode].AsStringName() == node)
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
			fromNode = con[_fromNode].AsStringName(),
			toNode = con[_toNode].AsStringName();
		
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
				if(con[_fromPort].AsInt32() == oldPort && con[_fromNode].AsStringName() == node)
				{
					DisconnectNode(con);
					ConnectNode(con[_fromNode].AsStringName(), newPort, con[_toNode].AsStringName(), con[_toPort].AsInt32());
				}
			}

			void CheckInPort()
			{
				if(con[_toPort].AsInt32() == oldPort && con[_toNode].AsStringName() == node)
				{
					DisconnectNode(con);
					ConnectNode(con[_fromNode].AsStringName(), con[_fromPort].AsInt32(), con[_toNode].AsStringName(), newPort);
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

			UndoRedo.AddDoMethod(this, Node.MethodName.RemoveChild, node);
			UndoRedo.AddUndoMethod(this, Node.MethodName.AddChild, node);
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
		if(_arrangingNodes)
		{
			_arrangingNodes = false;
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
		Dock.DisposeTree(DialogueTree);
	}

	private void OnDialogueTreeDataChanged(DialogueTreeData newTreeData)
	{
		Visible = newTreeData != null;

		LoadTree(newTreeData);
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

	private int GetChildCount(DialogueNodeData dialogueNodeData)
	{
		int count = 0;

		foreach(Node child in GetChildren())
			if(child is DialogueNode dialogueNode && dialogueNode.NodeData == dialogueNodeData)
				count ++;
	
		return count;
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
