using UnityEngine;

namespace TerrainSystem;

public readonly struct PartnerNode
{
	public readonly ushort OctTreeIndex;

	public readonly int NodeIndex;

	public readonly sbyte Depth;

	public static readonly PartnerNode Invalid = new PartnerNode(ushort.MaxValue, -1, -1);

	public bool IsValid
	{
		get
		{
			if (NodeIndex >= 0 && OctTreeIndex < ushort.MaxValue)
			{
				return Depth < 255;
			}
			return false;
		}
	}

	public PartnerNode(ushort octTreeIndex, int nodeIndex, sbyte depth)
	{
		OctTreeIndex = octTreeIndex;
		NodeIndex = nodeIndex;
		Depth = depth;
	}

	public bool IsLeaf()
	{
		return VoxelTerrain.ReadOnlyOctree.Octrees[OctTreeIndex].GetIsLeaf(NodeIndex);
	}

	public byte GetDensity()
	{
		return VoxelTerrain.ReadOnlyOctree.Octrees[OctTreeIndex].Get(NodeIndex);
	}

	public byte GetChildDensity(int childIndex)
	{
		return VoxelTerrain.ReadOnlyOctree.Octrees[OctTreeIndex].GetChildDensity(NodeIndex, childIndex);
	}

	public VoxelNodeType GetChildNodeType(int childIndex)
	{
		return VoxelTerrain.ReadOnlyOctree.Octrees[OctTreeIndex].GetChildNodeType(NodeIndex, childIndex);
	}

	public PartnerNode GetChildPartnerNode(int childIndex)
	{
		if (!IsValid)
		{
			return Invalid;
		}
		return new PartnerNode(OctTreeIndex, VoxelTerrain.ReadOnlyOctree.Octrees[OctTreeIndex].GetChildNodeIndex(NodeIndex, childIndex), (sbyte)(Depth + 1));
	}

	public void DrawChildren(Vector3Int parentNodePosition)
	{
		NodeStruct node = VoxelTerrain.ReadOnlyOctree.Octrees[OctTreeIndex].GetNode(NodeIndex, Depth, NodeIndex);
		for (int i = 0; i < 8; i++)
		{
			node.DrawChild(parentNodePosition, i);
		}
	}
}
