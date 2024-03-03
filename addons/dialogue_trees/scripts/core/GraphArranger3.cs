# if TOOLS

using Ardot.DialogueTrees.DialogueNodes;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ardot.DialogueTrees;

///<summary>Class for handling the arrangement of <c>GraphEdit</c>s.</summary>
public static class GraphArranger3
{
	///<summary>Arranges a <c>GraphEdit</c>'s <c>GraphNode</c>s.</summary>
	///<param name = 'graph'>The <c>GraphEdit</c> to arrange.</param>
	///<param name = 'arrangeMatch'>Condition that decides whether nodes should be included in being arranged. Mainly useful for including only selected nodes.</param>\
	///<param name = 'keepPositions'>Whether to keep the positions of each chunk of nodes.</param>
	///<param name = 'graphOrigin'>The pixel coordinates of the top-left corner of the arranged graph.</param>
	///<param name = 'cellSize'>The size in pixels of the cells used to position and arrange nodes.</param>
	///<param name = 'cellPadding'>The number of cells of distance to keep between adjacent nodes.</param>
	public static void ArrangeGraph(GraphEdit graph, Predicate<GraphNode> arrangeMatch = null, bool keepPositions = false, Vector2 graphOrigin = default, Vector2? cellSize = null, Vector2I? cellPadding = null)
	{
		arrangeMatch ??= (node) => true;

		GraphData graphData = new(graph, arrangeMatch);
		GraphArrangementOrderer graphOrderer = new(graph, graphData);

		GraphArrangementOrderer.NodeGroupArrangementData[][] graphArrangementSteps = graphOrderer.GetArrangementSteps();

		Vector2 _cellSize = cellSize ?? new Vector2(100, 50);
		Vector2I _cellPadding = cellPadding ?? new Vector2I(1, 1);

		Rect2 chunkRects = new (graphOrigin, Vector2.Zero);

		foreach (GraphArrangementOrderer.NodeGroupArrangementData[] chunkArrangementData in graphArrangementSteps)
		{
			GD.Print(chunkArrangementData.Length);

			List<GraphNode> arrangedNodes = new (chunkArrangementData.Length);
			HashSet<Vector2I> usedCells = new();

			Rect2 totalChunkRect = new (graphOrigin + new Vector2(0, chunkRects.Size.Y), Vector2.Zero);
			Rect2 chunkOverlapRect = default;

			foreach (GraphArrangementOrderer.NodeGroupArrangementData groupArrangementData in chunkArrangementData)
			{
				if (groupArrangementData.OutputNodesToArrange == null)
					continue;

				if (groupArrangementData.OriginNode == null)
				{
					GraphNode chunkOriginNode = groupArrangementData.OutputNodesToArrange[0];
					arrangedNodes.Add(chunkOriginNode);
					chunkOriginNode.PositionOffset = keepPositions ? chunkOriginNode.PositionOffset : totalChunkRect.Position;
					ForeachCoveredCell(chunkOriginNode, GetNodeGridPosition(chunkOriginNode, _cellSize), _cellSize, _cellPadding, (vec) => usedCells.Add(vec));  
					chunkOverlapRect = new Rect2(chunkOriginNode.PositionOffset, Vector2.Zero);
					totalChunkRect = totalChunkRect.Merge(new (chunkOriginNode.PositionOffset, chunkOriginNode.Size));
					continue;
				}

				Vector2I originNodePosition = GetNodeGridPosition(groupArrangementData.OriginNode, _cellSize);
				int outputNodesGridSize = GetNodesGridSize(groupArrangementData.OutputNodesToArrange, _cellSize, _cellPadding).Y;
				
				ArrangeNodes(
					groupArrangementData.OutputNodesToArrange, 
					(node) => originNodePosition + new Vector2I(GetNodeGridSize(groupArrangementData.OriginNode, _cellSize, _cellPadding).X, -outputNodesGridSize / 2 + GetNodeGridSize(node, _cellSize, _cellPadding).Y / 2)
				);

				int inputNodesGridSize = GetNodesGridSize(groupArrangementData.OutputNodesToArrange, _cellSize, _cellPadding).Y;
				
				ArrangeNodes(
					groupArrangementData.InputNodesToArrange, 
					(node) => originNodePosition - new Vector2I(GetNodeGridSize(node, _cellSize, _cellPadding).X, -inputNodesGridSize / 2 + GetNodeGridSize(node, _cellSize, _cellPadding).Y / 2)
				);

				void ArrangeNodes(GraphNode[] nodesToArrange, Func<GraphNode, Vector2I> findGridPosition)
				{
					int arrangedNodesGridSize = 0;

					foreach (GraphNode node in nodesToArrange)
					{
						arrangedNodes.Add(node);

						Vector2I gridPosition = findGridPosition.Invoke(node) + new Vector2I(0, arrangedNodesGridSize);
						int gridPositionYOffset = 0;

						bool IntersectsCoveredCells()
						{
							bool intersects = false;
							ForeachCoveredCell(node, new Vector2I(gridPosition.X, gridPosition.Y + gridPositionYOffset), _cellSize, _cellPadding, (vec) => intersects |= usedCells.Contains(vec));

							return intersects;
						}

						while (IntersectsCoveredCells())
							gridPositionYOffset = (gridPositionYOffset % 2 == 0 ? 1 : -1) * (Math.Abs(gridPositionYOffset) + 1);

						gridPosition += new Vector2I(0, gridPositionYOffset);

						node.PositionOffset = gridPosition * _cellSize;
						ForeachCoveredCell(node, gridPosition, _cellSize, _cellPadding, (vec) => usedCells.Add(vec));

						Rect2 nodeRect = new (node.PositionOffset, node.Size);

						if(nodeRect.Position.Y < chunkOverlapRect.Position.Y)
							chunkOverlapRect = chunkOverlapRect.Merge(new Rect2(nodeRect.Position, Vector2.Zero));

						totalChunkRect = totalChunkRect.Merge(new Rect2(node.PositionOffset, node.Size));
						arrangedNodesGridSize += GetNodeGridSize(node, _cellSize, _cellPadding).Y;
					}
				}
			}

			if(keepPositions)   
				continue;

			float chunkYOffset = chunkOverlapRect.Size.Y + _cellSize.Y * _cellPadding.Y;

			foreach(GraphNode graphNode in arrangedNodes)
				graphNode.PositionOffset += new Vector2(0, chunkYOffset);

			totalChunkRect = new Rect2(totalChunkRect.Position.X, totalChunkRect.Position.Y + chunkYOffset, totalChunkRect.Size);
			chunkRects = chunkRects.Merge(totalChunkRect);
		}
	}

	private static Vector2I GetNodeGridPosition(GraphNode node, Vector2 cellSize)
	{
		return (Vector2I)(node.PositionOffset / cellSize);
	}

	private static Vector2I GetNodesGridSize(GraphNode[] nodes, Vector2 cellSize, Vector2I cellPadding)
	{
		Vector2I gridSize = Vector2I.Zero;

		foreach (GraphNode node in nodes)
			gridSize += GetNodeGridSize(node, cellSize, cellPadding);

		return gridSize;
	}

	private static Vector2I GetNodeGridSize(GraphNode node, Vector2 cellSize, Vector2I cellPadding)
	{
		return (Vector2I)(node.Size / cellSize).Ceil() + cellPadding;
	}

	private static void ForeachCoveredCell(GraphNode node, Vector2I gridPosition, Vector2 cellSize, Vector2I cellPadding, Action<Vector2I> action)
	{
		gridPosition -= cellPadding;
		Vector2I coveredArea = GetNodeGridSize(node, cellSize, cellPadding);

		for (int x = 0; x < coveredArea.X; x++)
		{
			for (int y = 0; y < coveredArea.Y; y++)
			{
				Vector2I cellLocation = gridPosition + new Vector2I(x, y);

				action.Invoke(cellLocation);
			}
		}
	}

	///<summary>Class for finding the order that nodes in a <c>GraphEdit</c> should be arranged in.</summary>
	private class GraphArrangementOrderer
	{
		public GraphArrangementOrderer(GraphEdit graph, GraphData graphData)
		{
			_graph = graph;
			_graphData = graphData;
		}

		private readonly GraphEdit _graph;
		private readonly GraphData _graphData;

		///<summary>
		///Calculates the arrangement steps needed to arrange a graph. Steps are grouped into chunks, with each chunk representing
		///the steps to organise one group of nodes. <para/>
		///<b>Note:</b> The first <c>NodeGroupArrangementData</c> in each chunk will never have a preceding node, and will only have one output node to arrange.
		///</summary>
		public NodeGroupArrangementData[][] GetArrangementSteps()
		{
			GraphNode[] chunkOrigins = FindGraphChunkOrigins();
			NodeGroupArrangementData[][] arrangementSteps = new NodeGroupArrangementData[chunkOrigins.Length][];

			for (int x = 0; x < chunkOrigins.Length; x++)
				arrangementSteps[x] = GetChunkArrangementSteps(chunkOrigins[x]);

			Array.Sort(arrangementSteps, (a, b) => a.Length - b.Length);

			return arrangementSteps;

			///<summary>Gets the arrangement steps for a single graph chunk.</summary>
			NodeGroupArrangementData[] GetChunkArrangementSteps(GraphNode chunkOrigin)
			{
				Dictionary<GraphNode, NodeGroupArrangementData> groupArrangementSteps = new();

				Queue<NodeArrangementData> nodesToCheck = new();
				nodesToCheck.Enqueue(new NodeArrangementData(chunkOrigin, null, false));

				while (nodesToCheck.Count > 0)
				{
					NodeArrangementData nodeToCheck = nodesToCheck.Dequeue();

					if (groupArrangementSteps.ContainsKey(nodeToCheck.Node) && (nodeToCheck.DontArrangeIfAlreadyOrganised || groupArrangementSteps[nodeToCheck.Node].PrecedingNode == nodeToCheck.PrecedingNode))
					{
						if (nodeToCheck.DontArrangeIfAlreadyOrganised)
							RemovePreviousNodeConnection(nodeToCheck.PrecedingNode);

						continue;
					}

					Connection[] loopingConnections = FindLoopingConnections(nodeToCheck.Node);
					Connection[] nodeToCheckConnections = _graphData.GetConnectionsToNode(nodeToCheck.Node);

					List<GraphNode> inputNodesToArrange = new();
					List<int> inputNodesToArrangePort = new();

					List<GraphNode> outputNodesToArrange = new();
					List<int> outputNodesToArrangePort = new();

					foreach (Connection connection in nodeToCheckConnections)
					{
						if (outputNodesToArrange.Contains(connection.FromNode) || inputNodesToArrange.Contains(connection.FromNode))
							continue;

						bool isLoopingConnection = Array.IndexOf(loopingConnections, connection) != -1;

						if (connection.FromNode == nodeToCheck.Node && connection.ToNode != nodeToCheck.PrecedingNode && !outputNodesToArrange.Contains(connection.ToNode) && !inputNodesToArrange.Contains(connection.ToNode) && (!isLoopingConnection || !groupArrangementSteps.ContainsKey(connection.ToNode)))
						{
							outputNodesToArrange.Add(connection.ToNode);
							outputNodesToArrangePort.Add(connection.FromPort);
							nodesToCheck.Enqueue(new NodeArrangementData(connection.ToNode, nodeToCheck.Node, isLoopingConnection));
						}
						else if (connection.ToNode == nodeToCheck.Node && connection.FromNode != nodeToCheck.PrecedingNode && !outputNodesToArrange.Contains(connection.FromNode) && !inputNodesToArrange.Contains(connection.FromNode) && !groupArrangementSteps.ContainsKey(connection.ToNode))
						{
							inputNodesToArrange.Add(connection.FromNode);
							inputNodesToArrangePort.Add(connection.FromPort);
							nodesToCheck.Enqueue(new NodeArrangementData(connection.FromNode, nodeToCheck.Node, true));
						}
					}

					GraphNode[] inputNodesToArrangeArray = inputNodesToArrange.ToArray();
					Array.Sort(inputNodesToArrangePort.ToArray(), inputNodesToArrangeArray);

					GraphNode[] outputNodesToArrangeArray = outputNodesToArrange.ToArray();
					Array.Sort(outputNodesToArrangePort.ToArray(), outputNodesToArrangeArray);

					NodeGroupArrangementData nodeGroupArrangementData = new(outputNodesToArrangeArray, inputNodesToArrangeArray, nodeToCheck.Node, nodeToCheck.PrecedingNode);

					if (!groupArrangementSteps.ContainsKey(nodeToCheck.Node))
					{
						groupArrangementSteps.Add(nodeToCheck.Node, nodeGroupArrangementData);
						continue;
					}

					GraphNode previousPrecedingNode = groupArrangementSteps[nodeToCheck.Node].PrecedingNode;

					if (previousPrecedingNode != null)
						RemovePreviousNodeConnection(previousPrecedingNode);

					groupArrangementSteps[nodeToCheck.Node] = nodeGroupArrangementData;

					void RemovePreviousNodeConnection(GraphNode previousPrecedingNode)
					{
						NodeGroupArrangementData previousPrecedingNodeGroup = groupArrangementSteps[previousPrecedingNode];

						bool previousIsInput = previousPrecedingNodeGroup.InputNodesToArrange.Contains(nodeToCheck.Node);

						if(!previousIsInput && !previousPrecedingNodeGroup.OutputNodesToArrange.Contains(nodeToCheck.Node))
							return;

						GraphNode[] previousPrecedingNodeNodesToArrange = previousIsInput ? previousPrecedingNodeGroup.InputNodesToArrange : previousPrecedingNodeGroup.OutputNodesToArrange;

						if (previousPrecedingNodeNodesToArrange.Length == 0)
							return;

						GraphNode[] previousPrecedingNodeTrimmedNodesToArrange = new GraphNode[previousPrecedingNodeNodesToArrange.Length - 1];

						int x = 0;
						foreach (GraphNode graphNode in previousPrecedingNodeNodesToArrange)
						{
							if (graphNode == nodeToCheck.Node)
								continue;

							previousPrecedingNodeTrimmedNodesToArrange[x] = graphNode;
							x++;
						}

						groupArrangementSteps[previousPrecedingNode] = new NodeGroupArrangementData(
							previousIsInput ? previousPrecedingNodeGroup.OutputNodesToArrange : previousPrecedingNodeTrimmedNodesToArrange,
							previousIsInput ? previousPrecedingNodeTrimmedNodesToArrange : previousPrecedingNodeGroup.InputNodesToArrange,
							previousPrecedingNodeGroup.OriginNode,
							previousPrecedingNodeGroup.PrecedingNode
						);
					}
				}

				NodeGroupArrangementData[] organisedGroupArrangementSteps = new NodeGroupArrangementData[groupArrangementSteps.Count + 1];
				organisedGroupArrangementSteps[0] = new NodeGroupArrangementData(new GraphNode[] { chunkOrigin }, Array.Empty<GraphNode>(), null, null);

				int arrangementStepIndex = 1;
				Stack<GraphNode> nodesToGroup = new();
				nodesToGroup.Push(chunkOrigin);

				while (nodesToGroup.Count > 0)
				{
					GraphNode nodeToGroup = nodesToGroup.Pop();

					NodeGroupArrangementData nodeToGroupArrangementData = groupArrangementSteps[nodeToGroup];

					foreach (GraphNode graphNode in nodeToGroupArrangementData.OutputNodesToArrange.Concat(nodeToGroupArrangementData.InputNodesToArrange).Reverse())
						nodesToGroup.Push(graphNode);

					organisedGroupArrangementSteps[arrangementStepIndex] = nodeToGroupArrangementData;
					arrangementStepIndex++;
				}

				return organisedGroupArrangementSteps;
			}
		}

		///<summary>Finds the leftmost node of each seperate chunk in the graph.</summary>
		private GraphNode[] FindGraphChunkOrigins()
		{
			HashSet<GraphNode> chunkOrigins = new();

			foreach (GraphNode graphNode in _graphData.GetGraphNodes())
			{
				GraphNode leftmostConnectedNode = FindLeftmostConnectedNode(graphNode);

				if (!chunkOrigins.Contains(leftmostConnectedNode))
					chunkOrigins.Add(leftmostConnectedNode);
			}

			return chunkOrigins.ToArray();
		}

		///<summary>Finds all output connections from a node that eventually loop back to that node.</summary>
		private Connection[] FindLoopingConnections(GraphNode node)
		{
			Connection[] outputConnections = _graphData.GetConnectionsToNode(node, GraphData.ConnectionType.OutputConnection);
			List<Connection> loopingConnections = new();

			HashSet<GraphNode> checkedNodes = new();

			foreach (Connection connection in outputConnections)
			{
				Queue<GraphNode> nodesToCheck = new();
				nodesToCheck.Enqueue(connection.ToNode);

				while (nodesToCheck.Count > 0)
				{
					GraphNode nodeToCheck = nodesToCheck.Dequeue();

					foreach (Connection outConnection in _graphData.GetConnectionsToNode(nodeToCheck, GraphData.ConnectionType.OutputConnection))
					{
						GraphNode connectedNode = outConnection.ToNode;

						if (connectedNode == node)
						{
							loopingConnections.Add(connection);
							goto Continue;
						}

						if (checkedNodes.Contains(connectedNode))
							goto Continue;

						checkedNodes.Add(connectedNode);
						nodesToCheck.Enqueue(connectedNode);
					}
				}

			Continue:
				continue;
			}

			return loopingConnections.ToArray();
		}

		private GraphNode FindLeftmostConnectedNode(GraphNode node)
		{
			HashSet<GraphNode> checkedNodes = new();

			return FindLeftmostConnectedNodeRecursive(node, out _);

			GraphNode FindLeftmostConnectedNodeRecursive(GraphNode node, out int leftDistance)
			{
				leftDistance = 0;

				if (checkedNodes.Contains(node))
					return null;

				checkedNodes.Add(node);

				Connection[] nodeConnections = _graphData.GetConnectionsToNode(node);

				GraphNode furthestNode = null;
				int furthestDistance = 0;

				foreach (Connection connection in nodeConnections)
				{
					int distanceModifier = 1;
					GraphNode graphNode = connection.FromNode;

					if (graphNode == node)
					{
						graphNode = connection.ToNode;
						distanceModifier = -1;
					}

					if (checkedNodes.Contains(graphNode))
						continue;

					GraphNode leftmostNode = FindLeftmostConnectedNodeRecursive(graphNode, out int distance);

					if (leftmostNode != null && distance + distanceModifier > furthestDistance)
					{
						furthestNode = leftmostNode;
						furthestDistance = distance + distanceModifier;
					}
				}

				if (furthestNode == null)
				{
					leftDistance = 0;
					return node;
				}

				leftDistance = furthestDistance;
				return furthestNode;
			}
		}

		///<summary>Data type that contains data needed to organise a connected group of <c>GraphNode</c>s.</summary>
		public readonly struct NodeGroupArrangementData
		{
			public NodeGroupArrangementData(GraphNode[] outputNodesToArrange, GraphNode[] inputNodesToArrange, GraphNode originNode, GraphNode precedingNode)
			{
				OutputNodesToArrange = outputNodesToArrange;
				InputNodesToArrange = inputNodesToArrange;
				OriginNode = originNode;
				PrecedingNode = precedingNode;
			}

			///<summary>The node that OriginNode is organised by.</summary>
			public readonly GraphNode PrecedingNode;

			///<summary>The node that all <c>NodesToArrange</c> connect back to.</summary>
			public readonly GraphNode OriginNode;

			///<summary>Nodes to arrange that are connected to an output of the preceding node.</summary>
			public readonly GraphNode[] OutputNodesToArrange;

			///<summary>Nodes to arrange that are connected to an input of the preceding node.</summary>
			public readonly GraphNode[] InputNodesToArrange;
		}

		///<summary>Data type that contains data needed to organise a single <c>GraphNode</c>.</summary>
		public readonly struct NodeArrangementData
		{
			public NodeArrangementData(GraphNode node, GraphNode precedingNode, bool dontArrangeIfAlreadyOrganised)
			{
				Node = node;
				PrecedingNode = precedingNode;
				DontArrangeIfAlreadyOrganised = dontArrangeIfAlreadyOrganised;
			}

			public readonly GraphNode Node;
			public readonly GraphNode PrecedingNode;
			public readonly bool DontArrangeIfAlreadyOrganised;
		}
	}

	///<summary>Class for quickly accessing all nodes and connections in a <c>GraphEdit</c>.</summary>
	private class GraphData
	{
		private const string
		_fromNode = "from_node",
		_fromPort = "from_port",
		_toNode = "to_node",
		_toPort = "to_port";

		public GraphData(GraphEdit graph, Predicate<GraphNode> arrangeMatch)
		{
			_arrangeMatch = arrangeMatch;
			_graph = graph;

			Godot.Collections.Array<Godot.Collections.Dictionary> graphConnections = graph.GetConnectionList();
			Dictionary<GraphNode, List<Connection>> nodeConnectionsList = new();

			foreach (GraphNode graphNode in GetGraphNodes())
				nodeConnectionsList.Add(graphNode, new List<Connection>());

			foreach (Godot.Collections.Dictionary con in graphConnections)
			{
				Connection connection = new
				(
					graph.GetNode<GraphNode>(con[_fromNode].AsStringName().ToString()),
					con[_fromPort].AsInt32(),
					graph.GetNode<GraphNode>(con[_toNode].AsStringName().ToString()),
					con[_toPort].AsInt32()
				);

				if(!nodeConnectionsList.ContainsKey(connection.FromNode) || !nodeConnectionsList.ContainsKey(connection.ToNode))
					continue;

				nodeConnectionsList[connection.FromNode].Add(connection);
				nodeConnectionsList[connection.ToNode].Add(connection);
			}

			Dictionary<GraphNode, Connection[]> nodeConnections = new();

			foreach (GraphNode graphNode in nodeConnectionsList.Keys)
				nodeConnections.Add(graphNode, nodeConnectionsList[graphNode].ToArray());

			_nodeConnections = nodeConnections;
		}

		private readonly Predicate<GraphNode> _arrangeMatch;
		private readonly GraphEdit _graph;
		private readonly Dictionary<GraphNode, Connection[]> _nodeConnections;

		///<summary>Gets all connections to a node.</summary>
		public Connection[] GetConnectionsToNode(GraphNode node)
		{
			return _nodeConnections[node];
		}

		///<summary>Gets all connections to a node that are the correct <c>ConnectionType</c>.</summary>
		public Connection[] GetConnectionsToNode(GraphNode node, ConnectionType connectionType)
		{
			Connection[] nodeConnections = GetConnectionsToNode(node);
			List<Connection> filteredConnections = new();

			foreach (Connection connection in nodeConnections)
				if (connection.ToNode == node && connectionType == ConnectionType.InputConnection || connection.FromNode == node && connectionType == ConnectionType.OutputConnection)
					filteredConnections.Add(connection);

			return filteredConnections.ToArray();
		}

		///<summary>Returns all valid nodes in the graph.</summary>
		public GraphNode[] GetGraphNodes()
		{
			List<GraphNode> graphNodes = new ();

			foreach (Node node in _graph.GetChildren())
				if (node is GraphNode graphNode && _arrangeMatch.Invoke(graphNode))
					graphNodes.Add(graphNode);

			return graphNodes.ToArray();
		}

		public enum ConnectionType
		{
			InputConnection,
			OutputConnection,
		}
	}

	///<summary>Data type that defines a connection between two nodes in a <c>GraphEdit</c>.</summary>
	private readonly struct Connection
	{
		public Connection(GraphNode fromNode, int fromPort, GraphNode toNode, int toPort)
		{
			FromNode = fromNode;
			FromPort = fromPort;
			ToNode = toNode;
			ToPort = toPort;
		}

		public readonly GraphNode
		FromNode,
		ToNode;

		public readonly int
		FromPort,
		ToPort;
	}
}

#endif
