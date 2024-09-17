# if TOOLS

using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

namespace Ardot.DialogueTrees;

public partial class GraphArranger
{
    private const string 
	_fromNode = "from_node",
	_fromPort = "from_port",
	_toNode = "to_node",
	_toPort = "to_port";

    public GraphArranger(bool arrangeOnlySelected = false, bool keepNodePositions = false, Vector2? cellSize = null, Vector2I? cellPadding = null)
    {
        ArrangeOnlySelected = arrangeOnlySelected;
        KeepNodePositions = keepNodePositions;
        
        if(cellPadding != null)
            CellPadding = cellPadding.Value;

        if(cellSize != null)
		    CellSize = cellSize.Value;
    }

    public bool KeepNodePositions;
    public bool ArrangeOnlySelected;
    public Vector2I CellPadding = new (1, 1);
	public Vector2 CellSize = new (100, 25);

    private GraphEdit _graph;
    private readonly HashSet<GraphNode> _arrangedNodes = new ();
	private readonly HashSet<Vector2I> _coveredCells = new ();
	private readonly List<Rect2> _chunkRects = new ();

    private Vector2I DefaultNodePosition {get => (Vector2I)((_graph.ScrollOffset + _graph.Size/6) / _graph.Zoom / CellSize);}

    public void ArrangeGraph(GraphEdit graph)
	{
        _graph = graph;
		Array<GraphNode> graphNodes = new ();

		foreach(Node node in _graph.GetChildren())
			if(node is GraphNode graphNode && (!ArrangeOnlySelected || graphNode.Selected))
				graphNodes.Add(graphNode);

		foreach(GraphNode graphNode in graphNodes)
		{
			if(!_arrangedNodes.Contains(graphNode))
			{
				GraphNode leftmostNode = GetLeftmostConnectedNode(graphNode);
				Vector2I position = KeepNodePositions ? (Vector2I)(leftmostNode.PositionOffset / CellSize) : DefaultNodePosition;

                Rect2? chunkRect = ArrangeChunk(leftmostNode, position);

                if(chunkRect != null)
				    _chunkRects.Add(chunkRect.Value);
			}
		}

		_arrangedNodes.Clear();
        _coveredCells.Clear();
        _chunkRects.Clear();
	}

    private Rect2 ArrangeChunk(GraphNode startNode, Vector2I suggestedGridPosition)
    {
        while(_chunkRects.Find((rect) => rect.HasPoint(suggestedGridPosition * CellSize)) != default)
            suggestedGridPosition += Vector2I.Down;

        Queue<Tuple<GraphNode, Vector2I>> outputNodesToArrange = new ();
        Queue<Tuple<GraphNode, Vector2I>> inputNodesToArrange = new ();

        outputNodesToArrange.Enqueue(new(startNode, suggestedGridPosition));

        Rect2 chunkArea = new (startNode.PositionOffset, startNode.Size);

        while(outputNodesToArrange.Count > 0 || inputNodesToArrange.Count > 0)
        {
            if(outputNodesToArrange.Count > 0)
            {
                Tuple<GraphNode, Vector2I> outputArrangeData = outputNodesToArrange.Dequeue();
                ArrangeNode(outputArrangeData.Item1, outputArrangeData.Item2);
                continue;
            }

            Tuple<GraphNode, Vector2I> inputArrangeData = inputNodesToArrange.Dequeue();
            ArrangeNode(inputArrangeData.Item1, inputArrangeData.Item2);
        }

        void ArrangeNode(GraphNode arrangingNode, Vector2I arrangingNodeSuggestedGridPosition)
        {
            _arrangedNodes.Add(arrangingNode);

            GraphNode[] inputNodes = GetConnectedNodes(arrangingNode, SlotType.InPort);
            GraphNode[] outputNodes = GetConnectedNodes(arrangingNode, SlotType.OutPort);

            Vector2I finalGridPosition;

            for(int x = 0;; x++)
            {
                finalGridPosition = new (arrangingNodeSuggestedGridPosition.X, arrangingNodeSuggestedGridPosition.Y + x);

                bool coversExistingNodes = false;
                ForeachCoveredCell(arrangingNode, finalGridPosition, (cell) => coversExistingNodes |= _coveredCells.Contains(cell));

                if(coversExistingNodes)
                    continue;

                ForeachCoveredCell(arrangingNode, finalGridPosition, (cell) => _coveredCells.Add(cell));
                arrangingNode.PositionOffset = finalGridPosition * CellSize;
                break;
            }

            chunkArea = chunkArea.Merge(new Rect2(arrangingNode.PositionOffset, arrangingNode.Size));
            Vector2I arrangingNodeGridSize = GetNodeGridSize(arrangingNode);

            ArrangeConnectedNodes(outputNodes, finalGridPosition + new Vector2I(arrangingNodeGridSize.X, -(GetNodesGridSize(outputNodes).Y - arrangingNodeGridSize.Y) / 2), null, false);
            ArrangeConnectedNodes(inputNodes, finalGridPosition + new Vector2I(0, (GetNodesGridSize(inputNodes).Y - arrangingNodeGridSize.Y) / 2), (graphNode) => -GetNodeGridSize(graphNode), true);

            void ArrangeConnectedNodes(GraphNode[] connectedNodes, Vector2I suggestedPosition, Func<GraphNode, Vector2I> getExtraOffesets, bool inputOrOutput)
            {	
                for(int x = 0; x < connectedNodes.Length; x++)
                {
                    GraphNode graphNode = connectedNodes[x];

                    if(_arrangedNodes.Contains(graphNode) || (ArrangeOnlySelected && !graphNode.Selected))
                        continue;

                    _arrangedNodes.Add(graphNode);

                    Vector2I localSuggestedPosition = suggestedPosition + (getExtraOffesets != null ? getExtraOffesets.Invoke(connectedNodes[x]) : Vector2I.Zero);

                    if(inputOrOutput)
                    {
                        inputNodesToArrange.Enqueue(new (graphNode, localSuggestedPosition));
                        continue;
                    }

                    outputNodesToArrange.Enqueue(new (graphNode, localSuggestedPosition));
                }
            }
        }

        return chunkArea;
    }
    
    //Gets nodes that are connected to node, slotType cannot be any
    private GraphNode[] GetConnectedNodes(GraphNode node, SlotType nodeSlotType)
    {
        Array<Dictionary> connections = GetConnectionsToNode(node.Name, nodeSlotType);

        List<GraphNode> connectedNodesList = new ();
        List<int> connectedNodesIndexesList = new ();

        foreach(Dictionary con in connections)
        {
            GraphNode connectedNode = nodeSlotType switch
            {
                SlotType.InPort => _graph.GetNode<GraphNode>(con[_fromNode].AsStringName().ToString()),
                SlotType.OutPort => _graph.GetNode<GraphNode>(con[_toNode].AsStringName().ToString()),
                _ => throw new System.Exception("You can't set nodeSlotType to any."),
            };

            if(connectedNode != node)
            {		
                int existingIndex = connectedNodesList.IndexOf(connectedNode);

                if(existingIndex == -1)
                {	
                    connectedNodesList.Add(connectedNode);
                    connectedNodesIndexesList.Add(con[nodeSlotType == SlotType.InPort ? _toPort : _fromPort].AsInt32());
                }
                else
                    connectedNodesIndexesList[existingIndex] = (connectedNodesIndexesList[existingIndex] + con[nodeSlotType == SlotType.InPort ? _toPort : _fromPort].AsInt32()) / 2;	
            }
        }

        GraphNode[] connectedNodes = connectedNodesList.ToArray();
        int[] connectedNodesIndexes = connectedNodesIndexesList.ToArray();

        System.Array.Sort(connectedNodesIndexes, connectedNodes);

        return connectedNodes;
    }

    private GraphNode GetLeftmostConnectedNode(GraphNode node)
    {
        HashSet<GraphNode> checkedNodes = new ();

        GraphNode graphNode = GetLeftmostConnectedNodeRecursive(node, out int leftDistance);

        return graphNode;

        GraphNode GetLeftmostConnectedNodeRecursive(GraphNode node, out int leftDistance)
        {
            if(ArrangeOnlySelected && !node.Selected)
            {
                leftDistance = 0;
                return null;
            }

            checkedNodes.Add(node);

            GraphNode[] inputNodes = GetConnectedNodes(node, SlotType.InPort);
            GraphNode[] outputNodes = GetConnectedNodes(node, SlotType.OutPort);

            GraphNode furthest = null;
            int furthestDistance = 0;

            CompareLeftmostNode(outputNodes, -1);
            CompareLeftmostNode(inputNodes, 1);

            if(furthest == null)
            {
                leftDistance = 0;
                return node;
            }

            leftDistance = furthestDistance;
            return furthest;

            void CompareLeftmostNode(GraphNode[] graphNodes, int distanceModifier)
            {
                foreach(GraphNode graphNode in graphNodes)
                {
                    if(checkedNodes.Contains(graphNode))
                        continue;

                    GraphNode leftmostNode = GetLeftmostConnectedNodeRecursive(graphNode, out int distance);

                    if(leftmostNode != null && distance + distanceModifier > furthestDistance)
                    {
                        furthest = leftmostNode;
                        furthestDistance = distance + distanceModifier;
                    }
                }
            }
        }
    }

    private Vector2I GetNodesGridSize(GraphNode[] nodes)
    {
        Vector2I gridSize = Vector2I.Zero;

        foreach(GraphNode node in nodes)
            gridSize += GetNodeGridSize(node);

        return gridSize;
    }

    private Vector2I GetNodeGridSize(GraphNode node)
    {
        return (Vector2I)(node.Size / CellSize).Ceil() + CellPadding * 2;
    }

    private void ForeachCoveredCell(GraphNode node, Vector2I gridPosition, System.Action<Vector2I> action)
    {
        gridPosition -= CellPadding;
        Vector2I coveredArea = GetNodeGridSize(node);

        for(int x = 0; x < coveredArea.X; x++)
        {
            for(int y = 0; y < coveredArea.Y; y++)
            {
                Vector2I cellLocation = gridPosition + new Vector2I (x, y);

                action.Invoke(cellLocation);
            }
        }
    }

	private Array<Dictionary> GetConnectionsToNode(StringName node, SlotType portType)
	{
		Array<Dictionary> connections = new();

		foreach(Dictionary con in _graph.GetConnectionList())
		{
			switch (portType)
			{
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

	private enum SlotType
	{
		InPort,
		OutPort,
	}
}

# endif