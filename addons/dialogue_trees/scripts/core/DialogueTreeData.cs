using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

namespace Ardot.DialogueTrees;

///<summary>Stores all the data about a <c>DialogueTree</c> and its nodes.</summary>
[Tool]
[GlobalClass]
public partial class DialogueTreeData : Resource
{
	public DialogueTreeData()
	{
		DialogueTreesSettings settings = DialogueTreesSettings.Singleton;

		if(settings == null)
			return;

		if(settings.DefaultTree != null)
		{
			_dialogueNodeTypeNames = settings.DefaultTree._dialogueNodeTypeNames;
			_dialogueNodeTypes = settings.DefaultTree._dialogueNodeTypes;
			_connections = settings.DefaultTree._connections;
			_dialogueNodeSaveData = settings.DefaultTree._dialogueNodeSaveData;
			return;
		}
	}
	
	private Array<StringName> _dialogueNodeTypeNames = new ();
	private int[] _dialogueNodeTypes = System.Array.Empty<int>();
	private long[] _connections = System.Array.Empty<long>();
	private long[] _dialogueNodeIDs = System.Array.Empty<long>();
	private Array<Godot.Collections.Array> _dialogueNodeSaveData = new ();

	public IReadOnlyList<Godot.Collections.Array> DialogueNodeSaveData {get =>_dialogueNodeSaveData;} 
	public IReadOnlyList<long> DialogueNodeIDs {get => _dialogueNodeIDs;}

	public bool Empty
	{
		get => _dialogueNodeTypeNames.Count == 0;
	}

	public void Clear()
	{
		_dialogueNodeTypeNames.Clear();
		_dialogueNodeTypes = System.Array.Empty<int>();
		_connections = System.Array.Empty<long>();
		_dialogueNodeIDs = System.Array.Empty<long>();
		_dialogueNodeSaveData.Clear();
	}

	public int GetNodesCount() => _dialogueNodeTypes.Length;
	public StringName GetNodeType(int nodeIndex) => _dialogueNodeTypeNames[_dialogueNodeTypes[nodeIndex]];

	/// <summary>
	/// Saves a tree to this DialogueTreeData.
	/// </summary>
	/// <param name="nodeMatch">Predicate that should return true if a node should be saved.</param>
	public void SaveTree(DialogueGraph graph, bool saveUnique, Predicate<DialogueNode> nodeMatch = null)
	{
		Clear();

		List<long> oldNodeIDs = new ();

		if(saveUnique)
		{	
			foreach(DialogueNode node in graph.DialogueNodes.Values)
			{
				if(nodeMatch != null && !nodeMatch.Invoke(node))
					continue;
						
				oldNodeIDs.Add(node.ID);
				node.SetID(graph.Dock.DialogueNodeCount);
				graph.Dock.IncrementDialogueNodeCount();
			}
		}

		{
			foreach(DialogueNode node in graph.DialogueNodes.Values)
			{
				if(nodeMatch != null && !nodeMatch.Invoke(node))
					continue;

				_dialogueNodeSaveData.Add(node.Save());
			}

			int[] nodeTypeIndexes = new int[_dialogueNodeSaveData.Count];
			long[] nodeIDs = new long[_dialogueNodeSaveData.Count];
			System.Collections.Generic.Dictionary<StringName, int> types = new();

			{
				int x = 0;
				int nodeTypeIndexesIndex = 0;

				foreach(DialogueNode node in graph.DialogueNodes.Values)
				{
					if(nodeMatch != null && !nodeMatch.Invoke(node))
						continue;

					nodeIDs[x] = node.ID;
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

			_dialogueNodeTypeNames = new Array<StringName>(types.Keys);
			_dialogueNodeTypes = nodeTypeIndexes;
			_dialogueNodeIDs = nodeIDs;
		}

		Array<Dictionary> connectionsList = graph.GetConnectionList();
		List<long> connections = new ();

		for(int x = 0; x < connectionsList.Count; x++)
		{
			Dictionary con = connectionsList[x];

			DialogueNode
			fromNode = graph.GetNodeOrNull<DialogueNode>(con[DialogueGraph.FROM_NODE].AsStringName().ToString()),
			toNode = graph.GetNodeOrNull<DialogueNode>(con[DialogueGraph.TO_NODE].AsStringName().ToString());

			if(fromNode == null || toNode == null || nodeMatch != null && (!nodeMatch.Invoke(fromNode) || !nodeMatch.Invoke(toNode)))
				continue;

			connections.Add(fromNode.ID);
			connections.Add(con[DialogueGraph.FROM_PORT].AsInt32());
			connections.Add(toNode.ID);
			connections.Add(con[DialogueGraph.TO_PORT].AsInt32());
		}

		_connections = connections.ToArray();

		if(!string.IsNullOrEmpty(ResourcePath))
			ResourceSaver.Save(this, ResourcePath);

		if(saveUnique)
		{
			int index = 0;

			foreach(DialogueNode node in graph.DialogueNodes.Values)
			{
				if(nodeMatch != null && !nodeMatch.Invoke(node))
					continue;

				node.SetID(oldNodeIDs[index]);
			}
		}
	}

	public Array<DialogueNode> LoadTree(DialogueGraph graph, bool clearExistingTree = true, Array<DialogueNode> preLoadedNodes = null)
	{
		if(clearExistingTree)
			graph.ClearTree();

		Array<DialogueNode> loadedNodes = preLoadedNodes ?? new ();

		if(Empty && preLoadedNodes == null)
		{
			foreach (DialogueNodeData nodeData in DialogueTreesSettings.Singleton.DialogueNodeData)
			{
				for (int x = 0; x < nodeData.IncludeInNewTrees && x < nodeData.NodeLimit; x++)
				{
					DialogueNode node = graph.InstantiateDialogueNode(nodeData);

					if(node == null)
						continue;

					loadedNodes.Add(node);
					graph.AddDialogueNode(node);
				}
			}
		}
	
		if(preLoadedNodes == null)
			for (int x = 0; x < _dialogueNodeSaveData.Count; x++)
			{
				DialogueNode node = graph.InstantiateDialogueNode(DialogueTreesSettings.Singleton.GetDialogueNodeData(GetNodeType(x)), _dialogueNodeIDs[x]);

				if(node == null)
					continue;

				loadedNodes.Add(node);
				graph.AddDialogueNode(node);
				node?.Load(_dialogueNodeSaveData[x]);
			}
		else
			for(int x = 0; x < preLoadedNodes.Count; x++)
				graph.AddDialogueNode(preLoadedNodes[x]);
			
		for(int x = 0; x < GetConnectionsCount(); x++)
		{
			Connection con = GetConnection(x);

			DialogueNode fromNode = graph.DialogueNodes[con.FromNode];
			DialogueNode toNode = graph.DialogueNodes[con.ToNode];

			graph.ConnectNode(fromNode.Name, con.FromPort, toNode.Name, con.ToPort);
		}

		graph.SelectAllNodes(false);

		foreach(DialogueNode node in loadedNodes)
		{
			node.GraphReady();
			node.Selected = true;
		}		

		graph.ArrangingNodes = true;
		graph.CallDeferred(DialogueGraph.MethodName.ArrangeGraph, false);

		return loadedNodes;
	}


	///<summary>Unloads the given DialogueTreeData and removes all loadedNodes, this is intended only for use with UndoRedo.</summary>
	public void UnloadTree(DialogueGraph graph, Array<DialogueNode> loadedNodes)
	{			
		for(int x = 0; x < GetConnectionsCount(); x++)
		{
			Connection con = GetConnection(x);

			DialogueNode fromNode = graph.DialogueNodes[con.FromNode];
			DialogueNode toNode = graph.DialogueNodes[con.ToNode];

			graph.DisconnectNode(fromNode.Name, con.FromPort, toNode.Name, con.ToPort);
		}

		for(int x = 0; x < loadedNodes.Count; x++)
			graph.RemoveChild(loadedNodes[x]);
	}

	public int GetConnectionsCount() => _connections.Length / 4;
	public Connection GetConnection(int connectionIndex)
	{
		int index = connectionIndex * 4;

		return new (_connections[index], (int)_connections[index + 1], _connections[index + 2], (int)_connections[index + 3]);
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		return new ()
		{
			new ()
			{
				{"name", PropertyName._dialogueNodeTypeNames},
				{"type", (int)Variant.Type.Array},
				{"usage", (int)PropertyUsageFlags.Storage}
			},
			new ()
			{
				{"name", PropertyName._dialogueNodeTypes},
				{"type", (int)Variant.Type.PackedInt32Array},
				{"usage", (int)PropertyUsageFlags.Storage}
			},
			new ()
			{
				{"name", PropertyName._connections},
				{"type", (int)Variant.Type.PackedInt64Array},
				{"usage", (int)PropertyUsageFlags.Storage}
			},
			new ()
			{
				{"name", PropertyName._dialogueNodeIDs},
				{"type", (int)Variant.Type.PackedInt64Array},
				{"usage", (int)PropertyUsageFlags.Storage}
			},
			new ()
			{
				{"name", PropertyName._dialogueNodeSaveData},
				{"type", (int)Variant.Type.Array},
				{"usage", (int)PropertyUsageFlags.Storage}
			}
		};
	}

	public readonly struct Connection
{
	public Connection(long fromNode, int fromPort, long toNode, int toPort)
	{
		FromNode = fromNode;
		FromPort = fromPort;
		ToNode = toNode;
		ToPort = toPort;
	}

	public readonly int
	FromPort,
	ToPort;

	public readonly long
	FromNode,
	ToNode;
}
}
