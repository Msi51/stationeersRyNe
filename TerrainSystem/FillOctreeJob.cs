using System.Collections.Generic;
using UnityEngine;

namespace TerrainSystem;

public class FillOctreeJob : IThreadable
{
	private Node _startNode;

	private VoxelNodeType _fillType;

	private VoxelTerrain _voxelTerrain;

	public int ThreadCost => 1;

	public bool CanThread()
	{
		return true;
	}

	public string DebugName()
	{
		return "FillOctreeJob";
	}

	public FillOctreeJob(Node startNode, VoxelNodeType fillType)
	{
		_startNode = startNode;
		_fillType = fillType;
	}

	public void DoWork()
	{
		FillEmptyNodesBelowHeight(_startNode);
	}

	private void FillEmptyNodesBelowHeight(Node startNode)
	{
		Queue<Node> queue = new Queue<Node>();
		queue.Enqueue(startNode);
		while (queue.Count > 0)
		{
			Node node = queue.Dequeue();
			if (node.Children != null)
			{
				Node[] children = node.Children;
				foreach (Node item in children)
				{
					queue.Enqueue(item);
				}
				continue;
			}
			int size = node.GetSize();
			Vector3Int voxelPosition = node.GetVoxelPosition();
			Vector3Int vector3Int = voxelPosition + Vector3Int.one * size / 2;
			Vector3Int vector3Int2 = VoxelTerrain.OctreeToWorldSpace(voxelPosition, VoxelConstants.OriginOffsetInt) + Vector3Int.one * size / 2;
			float num = VoxelTerrainHeightmapImporter.ActualHeightArray[vector3Int.x, vector3Int.z];
			if ((float)vector3Int2.y < num && node.Density <= 0)
			{
				node.Density = byte.MaxValue;
				node.NodeType = _fillType;
			}
		}
	}
}
