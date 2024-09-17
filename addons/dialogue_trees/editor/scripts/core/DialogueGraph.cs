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
	private PackedScene _dialogueGraphOptionsButtonScene;

	public const string 
	FROM_NODE = "from_node",
	FROM_PORT = "from_port",
	TO_NODE = "to_node",
	TO_PORT = "to_port";

	public const int
	FROM_NODE_INDEX = 0,
	FROM_PORT_INDEX = 1,
	TO_NODE_INDEX = 2,
	TO_PORT_INDEX = 3;

	public EditorUndoRedoManager UndoRedo;
	public DialogueTreesPlugin Plugin;
	public DialogueTreeDock Dock;

	private Vector2[] _childPositions;

	public bool ArrangingNodes;

	private Button _optionsButton;
	private Button _arrangeSelectedNodesButton;
	private Button _arrangeAllNodesButton;

	private Array<DialogueNode> _dialogueNodes = new (); 
	public IReadOnlyList<DialogueNode> DialogueNodes {get => _dialogueNodes;}

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
		Node optionsButtonContainer = _dialogueGraphOptionsButtonScene.Instantiate();
		_optionsButton = optionsButtonContainer.GetNode<Button>("Button");
		Node arrangeNodesButtonContainer = _arrangeNodeButtonScene.Instantiate();
		_arrangeSelectedNodesButton = arrangeNodesButtonContainer.GetNode<Button>("ArrangeSelectedButton");
		_arrangeAllNodesButton = arrangeNodesButtonContainer.GetNode<Button>("ArrangeAllButton");

		graphMenu.AddChild(optionsButtonContainer);
		graphMenu.MoveChild(optionsButtonContainer, 0);
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

		_optionsButton.Pressed += OnOptionsButtonPressed;
		BeginNodeMove += OnNodeBeginMove;
		EndNodeMove += OnNodeEndMove;
		DeleteNodesRequest += OnDeleteNodesRequest;
		ConnectionRequest += OnConnectionRequested;
		DisconnectionRequest += OnDisonnectionRequested;
	}
	
    public int GetDialogueNodeIndex(DialogueNode dialogueNode)
	{
		return _dialogueNodes.IndexOf(dialogueNode);
	}

	///<summary>Instantiates and sets up a Dialogue Node. May return null. You must manually call GraphReady() and Load() on the returned node.</summary>
	public DialogueNode InstantiateDialogueNode(DialogueNodeData nodeData)
	{
		if(nodeData == null || _dialogueNodes.Count >= nodeData.NodeLimit || !nodeData.TryInstantiateDialogueNode(out DialogueNode dialogueNode))
			return null;

		dialogueNode.Setup(nodeData, this);

		HBoxContainer titlebarHBox = dialogueNode.GetTitlebarHBox();
		ValueButton deleteNodeButton = _deleteDialogueNodeButtonScene.Instantiate<ValueButton>();

		deleteNodeButton.Value = dialogueNode;
		deleteNodeButton.ValueButtonPressed += OnDeleteNodeButtonPressed;
		titlebarHBox.AddChild(deleteNodeButton);

		return dialogueNode;
	}

	public Array<DialogueNode> PreloadTreeData(DialogueTreeData treeData, out Array<int> connections)
	{
		Array<DialogueNode> loadedNodes = new ();

		if(treeData.IsEmpty() && DialogueNodes.Count == 0)
		{
			foreach (DialogueNodeData nodeData in DialogueTreesSettings.Singleton.DialogueNodeData)
			{
				for (int x = 0; x < nodeData.IncludeInNewTrees && x < nodeData.NodeLimit; x++)
				{
					DialogueNode node = InstantiateDialogueNode(nodeData);

					if(node == null)
						continue;

					loadedNodes.Add(node);
				}
			}
		}

		int nodeCount = DialogueNodes.Count;
		List<int> removedNodes = new ();

		int ConvertReference(int reference)
		{
			if (reference == -1)
				return -1;

			int offset = 0;

			for(int x = 0; x < removedNodes.Count; x++)
			{
				int removedNode = removedNodes[x];

				if(removedNode == reference)
					return -1;
				if(removedNode < reference)
					offset --;
			}

			return reference + nodeCount + offset;
		}

		for (int x = 0; x < treeData.DialogueNodeSaveData.Count; x++)
		{
			DialogueNode node = InstantiateDialogueNode(DialogueTreesSettings.Singleton.GetDialogueNodeData(treeData.GetNodeType(x)));

			if(node == null)
			{
				removedNodes.Add(x);
				continue;
			}

			loadedNodes.Add(node);
		}

		for (int x = 0; x < treeData.DialogueNodeSaveData.Count; x++)
		{
			int referenceIndex = ConvertReference(x);

			if(referenceIndex == -1)
				continue;

			DialogueNode node = loadedNodes[referenceIndex - nodeCount];

			Array<int> dialogueNodeReferences = treeData.DialogueNodeReferences[x].Duplicate();

			for(int y = 0; y < dialogueNodeReferences.Count; y++)
				dialogueNodeReferences[y] = ConvertReference(dialogueNodeReferences[y]);

			node.Load(new DialogueNodeSaveData(
				treeData.DialogueNodeSaveData[x],
				dialogueNodeReferences
			));
		}

		connections = new Array<int> ();

		for(int x = 0, connectionsCount = treeData.GetConnectionsCount(); x < connectionsCount; x++)
		{
			DialogueTreeData.Connection connection = treeData.GetConnection(x);

			int fromNode = ConvertReference(connection.FromNode);
			int toNode = ConvertReference(connection.ToNode);

			if(fromNode == -1 || toNode == -1)
				continue;

			connections.Add(fromNode);
			connections.Add(connection.FromPort);
			connections.Add(toNode);
			connections.Add(connection.ToPort);
		}
		
		return loadedNodes;
	}

	public void LoadTree(Array<DialogueNode> dialogueNodes, Array<int> connections)
	{
		foreach(DialogueNode dialogueNode in dialogueNodes)
			AddDialogueNode(dialogueNode);

		for(int x = 0; x < connections.Count; x += 4)
		{
			ConnectNode(
				_dialogueNodes[connections[x + FROM_NODE_INDEX]].Name, 
				connections[x + FROM_PORT_INDEX], 
				_dialogueNodes[connections[x + TO_NODE_INDEX]].Name, 
				connections[x + TO_PORT_INDEX]
			);
		}
	}

	public void UnloadTree(Array<DialogueNode> dialogueNodes, Array<int> connections)
	{			
		for(int x = 0; x < connections.Count; x += 4)
		{
			DisconnectNode(
				_dialogueNodes[connections[x + FROM_NODE_INDEX]].Name, 
				connections[x + FROM_PORT_INDEX], 
				_dialogueNodes[connections[x + TO_NODE_INDEX]].Name, 
				connections[x + TO_PORT_INDEX]
			);
		}

		for(int x = 0; x < dialogueNodes.Count; x++)
			RemoveDialogueNode(dialogueNodes[x]);
	}

	public void SaveTree(DialogueTreeData treeData, Predicate<DialogueNode> nodesToSaveMatch = null)
	{
		System.Collections.Generic.Dictionary<DialogueNode, int> nodesToSave = new ();

		{
			int x = 0;
			foreach(DialogueNode node in _dialogueNodes)
			{
				if(nodesToSaveMatch != null && !nodesToSaveMatch.Invoke(node))
					continue;

				nodesToSave.Add(node, x);
				x++;
			}
		}

		Array<Godot.Collections.Array> dialogueNodeSaveData = new ();
		Array<Array<int>> dialogueNodeReferences = new ();

		foreach(DialogueNode node in nodesToSave.Keys)
		{
			DialogueNodeSaveData saveData = node.Save();
			dialogueNodeSaveData.Add(saveData.General);

			if(saveData.References != null)
				for(int x = 0; x < saveData.References.Count; x++)
				{
					int reference = saveData.References[x];

					if(reference >= _dialogueNodes.Count || reference < 0)
					{
						saveData.References[x] = -1;
						continue;
					}

					saveData.References[x] = nodesToSave.TryGetValue(_dialogueNodes[saveData.References[x]], out int referenceInt) ? referenceInt : -1;
				}

			dialogueNodeReferences.Add(saveData.References);
		}

		int[] nodeTypeIndexes = new int[nodesToSave.Count];
		System.Collections.Generic.Dictionary<StringName, int> types = new();

		{
			int x = 0;
			int nodeTypeIndexesIndex = 0;

			foreach(DialogueNode node in nodesToSave.Keys)
			{
				StringName type = node.NodeData.DialogueNodeSaveName;

				if(!types.ContainsKey(type))
				{
					types.Add(type, x);
					x++;
				}

				nodeTypeIndexes[nodeTypeIndexesIndex] = types[type];
				nodeTypeIndexesIndex++;
			}
		}

		Array<Dictionary> connectionsList = GetConnectionList();
		List<int> connections = new ();

		for(int x = 0; x < connectionsList.Count; x++)
		{
			Dictionary con = connectionsList[x];

			DialogueNode
			fromNode = GetNodeOrNull<DialogueNode>(con[DialogueGraph.FROM_NODE].AsStringName().ToString()),
			toNode = GetNodeOrNull<DialogueNode>(con[DialogueGraph.TO_NODE].AsStringName().ToString());

			if(fromNode == null || toNode == null || !nodesToSave.ContainsKey(fromNode) || !nodesToSave.ContainsKey(toNode))
				continue;

			connections.Add(nodesToSave[fromNode]);
			connections.Add(con[DialogueGraph.FROM_PORT].AsInt32());
			connections.Add(nodesToSave[toNode]);
			connections.Add(con[DialogueGraph.TO_PORT].AsInt32());
		}

		treeData.SetValues(types.Keys.ToArray(), nodeTypeIndexes, dialogueNodeSaveData, dialogueNodeReferences, connections.ToArray());

		if(!string.IsNullOrEmpty(treeData.ResourcePath))
			ResourceSaver.Save(treeData, treeData.ResourcePath);
	}

	
	public Vector2 ToGraphSpace(Vector2 position)
	{	
		return (position + ScrollOffset) / Zoom;
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
		_dialogueNodes.Add(node);

		if(node != null)
			AddChild(node);
	}

	public void RemoveDialogueNode(DialogueNode dialogueNode)
	{
		_dialogueNodes.Remove(dialogueNode);
		RemoveChild(dialogueNode);
	}

	public void ClearTree()
	{
		ClearConnections();

		foreach(DialogueNode node in _dialogueNodes)
		{
			RemoveChild(node);
			node.QueueFree();
		}

		_dialogueNodes.Clear();
	}

	public void SelectAllNodes(bool selected = true)
	{
		foreach(DialogueNode node in _dialogueNodes)
			node.Selected = selected;
	}

	public void ArrangeGraph(bool arrangeOnlySelected = false)
	{
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

		UndoRedo.CreateAction("Delete Dialogue Nodes", Godot.UndoRedo.MergeMode.Disable, Dock.EditedDialogueTree);

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

		UndoRedo.CreateAction("Move Dialogue Nodes", Godot.UndoRedo.MergeMode.Disable, Dock.EditedDialogueTree);

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
		UndoRedo.CreateAction("Add Connection", Godot.UndoRedo.MergeMode.Disable, Dock.EditedDialogueTree);

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
		UndoRedo.CreateAction("Remove Connection", Godot.UndoRedo.MergeMode.Disable, Dock.EditedDialogueTree);
		UndoRedo.AddDoMethod(this, GraphEdit.MethodName.DisconnectNode, fromNode, (int)fromPort, toNode, (int)toPort);
		UndoRedo.AddUndoMethod(this, GraphEdit.MethodName.ConnectNode, fromNode, (int)fromPort, toNode, (int)toPort);
		UndoRedo.CommitAction();
	}

	private void OnOptionsButtonPressed()
	{	
		Dock.PopupMenu(new Rect2I((Vector2I)_optionsButton.GetScreenPosition() + new Vector2I (0, (int)_optionsButton.Size.Y), Vector2I.Zero));
	}

	private void OnDeleteNodeButtonPressed(Variant value)
	{
		OnDeleteNodesRequest(new Godot.Collections.Array() {((Node)value).Name});
	}

	[Flags]
	public enum SlotType
	{
		InPort = 1,
		OutPort = 2,
		Any = InPort | OutPort,
	}
}

# endif
