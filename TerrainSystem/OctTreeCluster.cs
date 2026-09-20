using Assets.Scripts.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TerrainSystem;

public class OctTreeCluster : IOctTree
{
	public const int MAX_OCTREE_DEPTH = 10;

	public const int MAX_OCTREE_SIZE = 1024;

	public readonly ReadonlyVoxelOctree[] Octrees;

	public int MaxDepth { get; }

	public static int GetOctreeCount(int maxDepth)
	{
		int num = Mathf.Max(0, maxDepth - 10);
		return (int)Mathf.Pow(8f, num);
	}

	public OctTreeCluster(int size)
	{
		MaxDepth = (sbyte)Mathf.Log(size, 2f);
		int octreeCount = GetOctreeCount(MaxDepth);
		Octrees = new ReadonlyVoxelOctree[octreeCount];
		for (int i = 0; i < Octrees.Length; i++)
		{
			Octrees[i] = new ReadonlyVoxelOctree(i);
		}
	}

	public void Clear()
	{
		for (int i = 0; i < Octrees.Length; i++)
		{
			Octrees[i]?.Clear();
			Octrees[i] = null;
		}
	}

	public byte GetRootDensity()
	{
		int num = 0;
		for (int i = 0; i < Octrees.Length; i++)
		{
			num += Octrees[i].GetRootDensity();
		}
		return (byte)(num / Octrees.Length);
	}

	public async UniTask Deserialize(string folderPath)
	{
		for (int i = 0; i < Octrees.Length; i++)
		{
			string path = $"{folderPath}/Terrain{i}.dat";
			WorkerCollections.LoadTerrainWorkers.Assign(new LoadTerrainJob(path, Octrees[i]));
		}
		int startJobCount = LoadTerrainWorker.GetTotalJobsEnqueued();
		WorkerCollections.LoadTerrainWorkers.ExecuteAll();
		while (WorkerCollections.LoadTerrainWorkers.IsAnyWorking())
		{
			await ImGuiLoadingScreen.SetProgress((float)(startJobCount - LoadTerrainWorker.GetTotalJobsEnqueued()) / (float)(startJobCount + 1));
		}
	}

	public NodeStruct GetNode(PartnerNode partnerNode, Vector3Int position, int desiredDepth)
	{
		int num = Mathf.Max(0, MaxDepth - 10);
		return Octrees[partnerNode.OctTreeIndex].GetNode(partnerNode, position, desiredDepth - num);
	}

	public NodeInfo Get(PartnerNode partnerNode, Vector3Int position, sbyte desiredDepth)
	{
		int num = Mathf.Max(0, MaxDepth - 10);
		return Octrees[partnerNode.OctTreeIndex].Get(partnerNode, position, desiredDepth - num);
	}

	public NodeInfo Get(Vector3Int vector3Int, sbyte desiredDepth)
	{
		int depth = 0;
		return Get(0, vector3Int.x, vector3Int.y, vector3Int.z, ref depth, desiredDepth);
	}

	public NodeInfo Get(int x, int y, int z, sbyte desiredDepth)
	{
		int depth = 0;
		return Get(0, x, y, z, ref depth, desiredDepth);
	}

	private NodeInfo Get(int index, int x, int y, int z, ref int depth, sbyte desiredDepth)
	{
		int num = Mathf.Max(0, MaxDepth - 10);
		if (desiredDepth < num)
		{
			return GetAggregateInfoAtDepth(x, y, z, desiredDepth);
		}
		if (num == 0)
		{
			return Octrees[index].Get(x, y, z, desiredDepth);
		}
		int num2 = 1 << MaxDepth - depth - 1;
		int num3 = (int)Mathf.Pow(8f, num - depth - 1);
		if (x >= num2)
		{
			index |= num3;
			x -= num2;
		}
		if (y >= num2)
		{
			index |= num3 * 2;
			y -= num2;
		}
		if (z >= num2)
		{
			index |= num3 * 4;
			z -= num2;
		}
		if (num2 == 1024)
		{
			return Octrees[index].Get(x, y, z, desiredDepth - num);
		}
		depth++;
		return Get(index, x, y, z, ref depth, desiredDepth);
	}

	private NodeInfo GetAggregateInfoAtDepth(int x, int y, int z, sbyte desiredDepth)
	{
		int num = Mathf.Max(0, MaxDepth - 10);
		int num2 = 0;
		if (desiredDepth == 0)
		{
			int num3 = 0;
			VoxelNodeType voxelNodeType = VoxelNodeType.None;
			for (int i = 0; i < Octrees.Length; i++)
			{
				NodeInfo rootInfo = Octrees[i].GetRootInfo();
				num3 += rootInfo.Density;
				voxelNodeType |= rootInfo.NodeType;
			}
			return new NodeInfo((byte)(num3 / Octrees.Length), voxelNodeType, desiredDepth);
		}
		int num4 = 1 << MaxDepth - desiredDepth - 1;
		int num5 = (int)Mathf.Pow(8f, num - desiredDepth - 1);
		while (x >= num4)
		{
			num2 |= num5;
			x -= num4;
		}
		while (y >= num4)
		{
			num2 |= num5 * 2;
			y -= num4;
		}
		while (z >= num4)
		{
			num2 |= num5 * 4;
			z -= num4;
		}
		if (num4 == 1024)
		{
			return Octrees[num2].Get(x, y, z, desiredDepth - num);
		}
		int num6 = 0;
		int num7 = 0;
		VoxelNodeType voxelNodeType2 = VoxelNodeType.None;
		for (int j = num2; j < num2 + num5; j++)
		{
			num7++;
			NodeInfo rootInfo2 = Octrees[j].GetRootInfo();
			num6 += rootInfo2.Density;
			voxelNodeType2 |= rootInfo2.NodeType;
		}
		return new NodeInfo((byte)((num7 > 0) ? ((byte)(num6 / num7)) : 0), voxelNodeType2, desiredDepth);
	}

	public PartnerNode GetPartnerNode(Node node)
	{
		Vector3Int voxelPosition = node.GetVoxelPosition();
		byte depth = node.Depth;
		return GetNodeAtDepth(0, voxelPosition.x, voxelPosition.y, voxelPosition.z, depth, 0);
	}

	private PartnerNode GetNodeAtDepth(int index, int x, int y, int z, int desiredDepth, int depth)
	{
		int num = Mathf.Max(0, MaxDepth - 10);
		if (desiredDepth < num)
		{
			return PartnerNode.Invalid;
		}
		if (num == 0)
		{
			return Octrees[0].GetPartnerNode(x, y, z, desiredDepth);
		}
		int num2 = 1 << MaxDepth - depth - 1;
		int num3 = (int)Mathf.Pow(8f, num - depth - 1);
		if (x >= num2)
		{
			index |= num3;
			x -= num2;
		}
		if (y >= num2)
		{
			index |= num3 * 2;
			y -= num2;
		}
		if (z >= num2)
		{
			index |= num3 * 4;
			z -= num2;
		}
		if (num2 == 1024)
		{
			return Octrees[index].GetPartnerNode(x, y, z, desiredDepth - num);
		}
		return GetNodeAtDepth(index, x, y, z, desiredDepth, depth + 1);
	}

	public int[] GetTerrainChecksums()
	{
		int[] array = new int[Octrees.Length];
		for (int i = 0; i < Octrees.Length; i++)
		{
			array[i] = Octrees[i].CheckSum;
		}
		return array;
	}
}
