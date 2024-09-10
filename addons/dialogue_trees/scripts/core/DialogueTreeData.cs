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
	private int[] _connections = System.Array.Empty<int>();
	private Array<Godot.Collections.Array> _dialogueNodeSaveData = new ();
	private Array<Array<int>> _dialogueNodeReferences = new ();

	public IReadOnlyList<Godot.Collections.Array> DialogueNodeSaveData {get =>_dialogueNodeSaveData;} 
	public IReadOnlyList<Array<int>> DialogueNodeReferences {get => _dialogueNodeReferences;}

	public void Clear()
	{
		_dialogueNodeTypeNames = System.Array.Empty<StringName>();
		_dialogueNodeTypes = System.Array.Empty<int>();
		_connections = 	System.Array.Empty<int>();
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

		System.Collections.Generic.Dictionary<DialogueNode, int> nodesToSave = new ();
		{
			int x = 0;
			foreach(DialogueNode node in graph.DialogueNodes)
			{
				if(nodeMatch != null && !nodeMatch.Invoke(node))
					continue;

				nodesToSave.Add(node, x);
				x++;
			}
		}

		int ConvertReference(DialogueNode referenceNode)
		{
			return nodesToSave.TryGetValue(referenceNode, out int referenceInt) ? referenceInt : -1;	
		}

		{
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

						if(reference >= graph.DialogueNodes.Count || reference < 0)
						{
							saveData.References[x] = -1;
							continue;
						}

						saveData.References[x] = ConvertReference(graph.DialogueNodes[saveData.References[x]]);
					}

				dialogueNodeReferences.Add(saveData.References);
			}

			_dialogueNodeSaveData = dialogueNodeSaveData;
			_dialogueNodeReferences = dialogueNodeReferences;
		}

		{
			int[] nodeTypeIndexes = new int[nodesToSave.Count];
			System.Collections.Generic.Dictionary<StringName, int> types = new();

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

			_dialogueNodeTypeNames = types.Keys.ToArray();
			_dialogueNodeTypes = nodeTypeIndexes;
		}

		Array<Dictionary> connectionsList = graph.GetConnectionList();
		List<int> connections = new ();

		for(int x = 0; x < connectionsList.Count; x++)
		{
			Dictionary con = connectionsList[x];

			DialogueNode
			fromNode = graph.GetNodeOrNull<DialogueNode>(con[DialogueGraph.FROM_NODE].AsStringName().ToString()),
			toNode = graph.GetNodeOrNull<DialogueNode>(con[DialogueGraph.TO_NODE].AsStringName().ToString());

			if(fromNode == null || toNode == null || !nodesToSave.ContainsKey(fromNode) || !nodesToSave.ContainsKey(toNode))
				continue;

			connections.Add(ConvertReference(fromNode));
			connections.Add(con[DialogueGraph.FROM_PORT].AsInt32());
			connections.Add(ConvertReference(toNode));
			connections.Add(con[DialogueGraph.TO_PORT].AsInt32());
		}

		_connections = connections.ToArray();

		if(!string.IsNullOrEmpty(ResourcePath))
			ResourceSaver.Save(this, ResourcePath);
	}

	public Array<DialogueNode> LoadTree(DialogueGraph graph, bool clearExistingTree = true, Array<DialogueNode> preLoadedNodes = null, Array<int> preRemovedNodes = null) => LoadTree(graph, out int _, out Array<int> _, clearExistingTree, preLoadedNodes, preRemovedNodes);
	
	public Array<DialogueNode> LoadTree(DialogueGraph graph, out int nodeCount, out Array<int> removedNodes, bool clearExistingTree = true, Array<DialogueNode> preLoadedNodes = null, Array<int> preRemovedNodes = null)
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

		nodeCount = graph.DialogueNodes.Count;
	
		if(preLoadedNodes == null)
		{
			removedNodes = new ();

			for (int x = 0; x < _dialogueNodeSaveData.Count; x++)
			{
				DialogueNode node = graph.InstantiateDialogueNode(DialogueTreesSettings.Singleton.GetDialogueNodeData(GetNodeType(x)));

				if(node == null)
				{
					removedNodes.Add(x);
					continue;
				}

				loadedNodes.Add(node);
				graph.AddDialogueNode(node);
			}

			for(int x = 0; x < _dialogueNodeSaveData.Count; x++)
			{
				int nodeIndex = ConvertIndex(x, nodeCount, removedNodes);

				if(nodeIndex == -1)
					continue;

				Array<int> dialogueNodeReferences = _dialogueNodeReferences[x].Duplicate();

				for(int y = 0; y < dialogueNodeReferences.Count; y++)
					dialogueNodeReferences[y] = ConvertIndex(dialogueNodeReferences[y], nodeCount, removedNodes);

				graph.DialogueNodes[nodeIndex].Load(new DialogueNodeSaveData(
					_dialogueNodeSaveData[x],
					dialogueNodeReferences
				));
			}
		}
		else
		{
			for(int x = 0; x < preLoadedNodes.Count; x++)
				graph.AddDialogueNode(preLoadedNodes[x]);

			removedNodes = preRemovedNodes;
		}
			
		for(int x = 0; x < GetConnectionsCount(); x++)
		{
			Connection con = GetConnection(x);

			int fromNodeIndex = ConvertIndex(con.FromNode, nodeCount, removedNodes);
			int toNodeIndex = ConvertIndex(con.ToNode, nodeCount, removedNodes);
			
			if(fromNodeIndex == -1 || toNodeIndex == -1)
				continue;

			graph.ConnectNode(graph.DialogueNodes[fromNodeIndex].Name, con.FromPort, graph.DialogueNodes[toNodeIndex].Name, con.ToPort);
		}

		graph.SelectAllNodes(false);
	
		foreach(DialogueNode node in loadedNodes)
		{
			if(preLoadedNodes == null)
				node.GraphReady();
				
			node.Selected = true;
		}		

		graph.ArrangingNodes = true;
		graph.CallDeferred(DialogueGraph.MethodName.ArrangeGraph, false);

		return loadedNodes;
	}

	///<summary>Unloads the given DialogueTreeData and removes all loadedNodes, this is intended only for use with UndoRedo.</summary>
	public void UnloadTree(DialogueGraph graph, Array<DialogueNode> loadedNodes, int nodeCount, Array<int> removedNodes)
	{			
		for(int x = 0; x < GetConnectionsCount(); x++)
		{
			Connection con = GetConnection(x);

			int fromNode = ConvertIndex(con.FromNode, nodeCount, removedNodes);
			int toNode = ConvertIndex(con.ToNode, nodeCount, removedNodes);

			if(fromNode == -1 || toNode == -1)
				continue;

			graph.DisconnectNode(graph.DialogueNodes[fromNode].Name, con.FromPort, graph.DialogueNodes[toNode].Name, con.ToPort);
		}

		for(int x = 0; x < loadedNodes.Count; x++)
			graph.RemoveDialogueNode(loadedNodes[x]);
	}

	public int GetConnectionsCount() => _connections.Length / 4;
	public Connection GetConnection(int connectionIndex)
	{
		int index = connectionIndex * 4;

		return new (_connections[index], (int)_connections[index + 1], _connections[index + 2], (int)_connections[index + 3]);
	}

	public bool IsValid()
	{
		int nodeCount = _dialogueNodeTypes.Length;

		bool typesValid = true;
		
		foreach(int type in _dialogueNodeTypes)
			typesValid &= _dialogueNodeTypeNames.Length > type && type >= 0;

		return
		typesValid &&
		_dialogueNodeReferences.Count == nodeCount &&
		_dialogueNodeSaveData.Count == nodeCount &&
		_connections.Length % 4 == 0;
	}	

	private static int ConvertIndex(int index, int nodeCount, Array<int> removedNodes)
	{
		if (index != -1)
		{
			int offset = 0;

			for(int x = 0; x < removedNodes.Count; x++)
			{
				int removedNode = removedNodes[x];

				if(removedNode == index)
					return -1;
				if(removedNode < index)
					offset --;
			}

			return index + nodeCount + offset;
		}
		else
			return -1;
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
				{"type", (int)Variant.Type.PackedInt32Array},
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
		public Connection(int fromNode, int fromPort, int toNode, int toPort)
		{
			FromNode = fromNode;
			FromPort = fromPort;
			ToNode = toNode;
			ToPort = toPort;
		}

		public readonly int
		FromPort,
		ToPort,
		FromNode,
		ToNode;
	}
}
