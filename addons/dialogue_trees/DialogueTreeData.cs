using Godot;
using Godot.Collections;

namespace Ardot.DialogueTrees;

///<summary>Stores all the data about a <c>DialogueTree</c> and its nodes.</summary>
[Tool]
[GlobalClass]
public partial class DialogueTreeData : Resource
{
    public DialogueTreeData()
    {
        DialogueTreeSettings settings = DialogueTreeSettings.Singleton;

        if(settings == null)
            return;

        if(settings.DefaultTree != null)
        {
            _dialogueNodeTypeNames = settings.DefaultTree._dialogueNodeTypeNames;
            _dialogueNodeTypes = settings.DefaultTree._dialogueNodeTypes;
            _connections = settings.DefaultTree._connections;
            DialogueNodeSaveData = settings.DefaultTree.DialogueNodeSaveData;
            return;
        }

        Array<StringName> nodeTypes = new ();

        foreach (DialogueNodeData nodeData in settings.DialogueNodeData)
        {
            for (int x = 0; x < nodeData.IncludeInNewTrees && x < nodeData.NodeLimit; x++)
            {
                nodeTypes.Add(nodeData.DialogueNodeSaveName);
                DialogueNodeSaveData.Add(null);
            }
        }

        SetNodeTypes(nodeTypes);
    }

    [Export]
    private Array<StringName> _dialogueNodeTypeNames = new ();

    [Export]
    private int[] _dialogueNodeTypes = System.Array.Empty<int>();

    [Export]
    private int[] _connections = System.Array.Empty<int>();

    [Export]
	///<summary>A collection of all data saved by each <c>DialogueNode</c>. This collection is always the same length as <c>DialogueNodeTypes</c>.</summary>
	public Array<Array> DialogueNodeSaveData = new ();

    public void Clear()
    {
        _dialogueNodeTypeNames.Clear();
        _dialogueNodeTypes = System.Array.Empty<int>();
        _connections = System.Array.Empty<int>();
        DialogueNodeSaveData.Clear();
    }

    public int GetNodesCount() => _dialogueNodeTypes.Length;
    public StringName GetNodeType(int nodeIndex) => _dialogueNodeTypeNames[_dialogueNodeTypes[nodeIndex]];

    public void SetNodeTypes(Array<StringName> nodeTypes)
    {
        System.Collections.Generic.Dictionary<StringName, int> types = new();

        int typeIndex = 0;

        foreach(StringName type in nodeTypes)
        {
            if(types.ContainsKey(type))
                continue;

            types.Add(type, typeIndex);
            typeIndex ++;
        }

        int[] nodeTypeIndexes = new int[nodeTypes.Count];

        for(int x = 0; x < nodeTypes.Count; x++)
            nodeTypeIndexes[x] = types[nodeTypes[x]];

        _dialogueNodeTypeNames = new Array<StringName>(types.Keys);
        _dialogueNodeTypes = nodeTypeIndexes;
    }

    public int GetConnectionsCount() => _connections.Length / 4;
    public Connection GetConnection(int connectionIndex)
    {
        int index = connectionIndex * 4;

        return new (_connections[index], _connections[index + 1], _connections[index + 2], _connections[index + 3]);
    }
    
    public void SetConnections(Connection[] connections)
    {
        int[] intConnections = new int[connections.Length * 4];

        for(int x = 0; x < connections.Length; x++)
        {
            int index = x * 4;
            Connection connection = connections[x];

            intConnections[index] = connection.FromNode;
            intConnections[index + 1] = connection.FromPort;
            intConnections[index + 2] = connection.ToNode;
            intConnections[index + 3] = connection.ToPort;
        }

        _connections = intConnections;
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
        FromNode,
        FromPort,
        ToNode,
        ToPort;
    }
}
