using System.Collections.Generic;
using Assets.Scripts.Serialization;

namespace Objects.Rockets;

public static class SpaceMapPathFinder
{
	public class PathNode
	{
		public readonly SpaceMapNode Node;

		public readonly PathNode Parent;

		public readonly float Distance;

		public PathNode(SpaceMapNode node, float distance, PathNode parent)
		{
			Node = node;
			Distance = distance;
			Parent = parent;
		}
	}

	private static readonly Queue<PathNode> _queue = new Queue<PathNode>(20);

	private static readonly HashSet<SpaceMapNode> _visited = new HashSet<SpaceMapNode>(20);

	public static bool GetNextConnection(SpaceMapNode current, SpaceMapNode target, out NodeTransit next, out List<NodeTransit> overallPath)
	{
		if (!FindPath(current, target, out var path))
		{
			next = null;
			overallPath = null;
			return false;
		}
		List<NodeTransit> list = path;
		next = list[list.Count - 1];
		overallPath = path;
		return true;
	}

	public static bool GetPath(SpaceMapNode current, SpaceMapNode target, out List<NodeTransit> path)
	{
		return FindPath(current, target, out path);
	}

	private static bool FindPath(SpaceMapNode current, SpaceMapNode target, out List<NodeTransit> path)
	{
		_queue.Clear();
		_visited.Clear();
		if (current == null || target == null)
		{
			path = null;
			return false;
		}
		if (current == target)
		{
			path = null;
			return false;
		}
		PathNode item = new PathNode(current, float.PositiveInfinity, null);
		_queue.Enqueue(item);
		while (_queue.Count > 0)
		{
			PathNode pathNode = _queue.Dequeue();
			if (pathNode.Node == target)
			{
				path = ReconstructPath(pathNode);
				return true;
			}
			foreach (NodeConnection childConnection in pathNode.Node.ChildConnections)
			{
				if (!_visited.Contains(childConnection.Child))
				{
					PathNode item2 = new PathNode(childConnection.Child, childConnection.Distance(), pathNode);
					_queue.Enqueue(item2);
				}
			}
			if (pathNode.Node.ParentConnection != null)
			{
				if (_visited.Contains(pathNode.Node.ParentConnection.Parent))
				{
					continue;
				}
				NodeConnection parentConnection = pathNode.Node.ParentConnection;
				PathNode item3 = new PathNode(parentConnection.Parent, parentConnection.Distance(), pathNode);
				_queue.Enqueue(item3);
			}
			_visited.Add(pathNode.Node);
		}
		path = null;
		return false;
	}

	private static List<NodeTransit> ReconstructPath(PathNode pathNode)
	{
		List<NodeTransit> list = new List<NodeTransit>();
		PathNode pathNode2 = pathNode;
		while (pathNode2.Parent != null)
		{
			list.Add(new NodeTransit(pathNode2.Parent.Node, pathNode2.Node, pathNode2.Distance));
			pathNode2 = pathNode2.Parent;
		}
		return list;
	}
}
