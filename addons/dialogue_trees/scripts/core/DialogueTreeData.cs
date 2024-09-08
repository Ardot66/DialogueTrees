using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

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
	
	private StringName[] _dialogueNodeTypeNames = System.Array.Empty<StringName>();
	private int[] _dialogueNodeTypes = System.Array.Empty<int>();
	private long[] _connections = System.Array.Empty<long>();
	private long[] _dialogueNodeIDs = System.Array.Empty<long>();
	private Array<Godot.Collections.Array> _dialogueNodeSaveData = new ();
	private Array<Array<long>> _dialogueNodeReferences = new ();

	public IReadOnlyList<Godot.Collections.Array> DialogueNodeSaveData {get =>_dialogueNodeSaveData;} 
	public IReadOnlyList<long> DialogueNodeIDs {get => _dialogueNodeIDs;}
	public IReadOnlyList<Array<long>> DialogueNodeReferences {get => _dialogueNodeReferences;}

	public void Clear()
	{
		_dialogueNodeTypeNames = System.Array.Empty<StringName>();
		_dialogueNodeTypes = System.Array.Empty<int>();
		_connections = 	System.Array.Empty<long>();
		_dialogueNodeIDs = System.Array.Empty<long>();
		_dialogueNodeSaveData.Clear();
		_dialogueNodeReferences.Clear();
	}

	public int GetNodesCount() => _dialogueNodeTypes.Length;
	public StringName GetNodeType(int nodeIndex) => _dialogueNodeTypeNames[_dialogueNodeTypes[nodeIndex]];

	/// <summary>
	/// Saves a tree to this DialogueTreeData.
	/// </summary>
	/// <param name="nodeMatch">Predicate that should return true if a node should be saved.</param>
	public void SaveTree(DialogueGraph graph, Predicate<DialogueNode> nodeMatch = null)
	{
		Clear();

		HashSet<DialogueNode> nodesToSave = new ();

		foreach(DialogueNode node in graph.DialogueNodes.Values)
		{
			if(nodeMatch != null && !nodeMatch.Invoke(node))
				continue;

			nodesToSave.Add(node);
		}

		{
			Array<Godot.Collections.Array> dialogueNodeSaveData = new ();
			Array<Array<long>> dialogueNodeReferences = new ();

			foreach(DialogueNode node in nodesToSave)
			{
				DialogueNodeSaveData saveData = node.Save();
				dialogueNodeSaveData.Add(saveData.General);
				dialogueNodeReferences.Add(saveData.References);
			}

			_dialogueNodeSaveData = dialogueNodeSaveData;
			_dialogueNodeReferences = dialogueNodeReferences;
		}

		{
			int[] nodeTypeIndexes = new int[nodesToSave.Count];
			long[] nodeIDs = new long[nodesToSave.Count];
			System.Collections.Generic.Dictionary<StringName, int> types = new();

			int x = 0;
			int nodeTypeIndexesIndex = 0;

			foreach(DialogueNode node in nodesToSave)
			{
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

			_dialogueNodeTypeNames = types.Keys.ToArray();
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
	}

	public Array<DialogueNode> LoadTree(DialogueGraph graph, bool clearExistingTree = true, Array<DialogueNode> preLoadedNodes = null)
	{
		if(clearExistingTree)
			graph.ClearTree();

		Array<DialogueNode> loadedNodes = preLoadedNodes ?? new ();

		if(_dialogueNodeTypeNames.Length == 0 && preLoadedNodes == null)
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

		System.Collections.Generic.Dictionary<long, long> IDConversions = new ();

		foreach(long ID in _dialogueNodeIDs)
		{
			if(!clearExistingTree)
			{
				IDConversions.Add(ID, graph.Dock.DialogueNodeCount);
				graph.Dock.IncrementDialogueNodeCount();
			}
			else
				IDConversions.Add(ID, ID);
		}
	
		if(preLoadedNodes == null)
			for (int x = 0; x < _dialogueNodeSaveData.Count; x++)
			{
				DialogueNode node = graph.InstantiateDialogueNode(DialogueTreesSettings.Singleton.GetDialogueNodeData(GetNodeType(x)), IDConversions[_dialogueNodeIDs[x]]);

				if(node == null)
					continue;

				loadedNodes.Add(node);
				graph.AddDialogueNode(node);

				Array<long> dialogueNodeReferences = _dialogueNodeReferences[x].Duplicate();

				for(int y = 0; y < dialogueNodeReferences.Count; y++)
					if(IDConversions.ContainsKey(dialogueNodeReferences[y]))
						dialogueNodeReferences[y] = IDConversions[dialogueNodeReferences[y]];
					else
						dialogueNodeReferences[y] = -1;

				node.Load(new DialogueNodeSaveData(
					_dialogueNodeSaveData[x],
					dialogueNodeReferences
				));
			}
		else
			for(int x = 0; x < preLoadedNodes.Count; x++)
				graph.AddDialogueNode(preLoadedNodes[x]);
			
		for(int x = 0; x < GetConnectionsCount(); x++)
		{
			Connection con = GetConnection(x);
			graph.ConnectNode(graph.DialogueNodes[IDConversions[con.FromNode]].Name, con.FromPort, graph.DialogueNodes[IDConversions[con.ToNode]].Name, con.ToPort);
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
			},
			new ()
			{
				{"name", PropertyName._dialogueNodeReferences},
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
