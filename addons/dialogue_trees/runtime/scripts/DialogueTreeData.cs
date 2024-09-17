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
	
	[Export]
	private StringName[] _dialogueNodeTypeNames = System.Array.Empty<StringName>();

	[Export]
	private int[] _dialogueNodeTypes = System.Array.Empty<int>();

	[Export]
	private Array<Godot.Collections.Array> _dialogueNodeSaveData = new ();

	[Export]
	private Array<Array<int>> _dialogueNodeReferences = new ();

	[Export]
	private int[] _connections = System.Array.Empty<int>();

	public IReadOnlyList<Godot.Collections.Array> DialogueNodeSaveData {get =>_dialogueNodeSaveData;} 
	public IReadOnlyList<Array<int>> DialogueNodeReferences {get => _dialogueNodeReferences;}
	public IReadOnlyList<int> Connections {get => _connections;}

	public void Clear()
	{
		_dialogueNodeTypeNames = System.Array.Empty<StringName>();
		_dialogueNodeTypes = System.Array.Empty<int>();
		_connections = 	System.Array.Empty<int>();
		_dialogueNodeSaveData.Clear();
		_dialogueNodeReferences.Clear();
	}

	public void SetValues(StringName[] dialogueNodeTypeNames, int[] dialogueNodeTypes, Array<Godot.Collections.Array> dialogueNodeSaveData, Array<Array<int>> dialogueNodeReferences, int[] connections)
	{
		_dialogueNodeTypeNames = dialogueNodeTypeNames;
		_dialogueNodeTypes = dialogueNodeTypes;
		_connections = connections;
		_dialogueNodeSaveData = dialogueNodeSaveData;
		_dialogueNodeReferences = dialogueNodeReferences;
	}

	public bool IsEmpty()
	{
		return _dialogueNodeTypeNames.Length == 0;
	}

	public int GetNodesCount() => _dialogueNodeTypes.Length;
	public StringName GetNodeType(int nodeIndex) => _dialogueNodeTypeNames[_dialogueNodeTypes[nodeIndex]];

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
