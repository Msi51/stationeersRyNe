using System;
using Assets.Scripts.UI.ImGuiUi;
using UnityEngine;

namespace TerrainSystem;

public readonly struct NodeStruct : INode
{
	public readonly int Index;

	public readonly bool IsLeaf;

	public readonly sbyte Depth;

	private readonly int _child0IndexOffset;

	private readonly int _child1IndexOffset;

	private readonly int _child2IndexOffset;

	private readonly int _child3IndexOffset;

	private readonly int _child4IndexOffset;

	private readonly int _child5IndexOffset;

	private readonly int _child6IndexOffset;

	private readonly int _child7IndexOffset;

	private readonly int _parentIndex;

	public static NodeStruct Invalid = new NodeStruct(null, -1, isLeaf: false, 0, VoxelNodeType.None, -1);

	public readonly ReadonlyVoxelOctree Parent;

	public byte Density { get; }

	public VoxelNodeType NodeType { get; }

	public int Child0Index => Index + _child0IndexOffset;

	public int Child1Index => Index + _child1IndexOffset;

	public int Child2Index => Index + _child2IndexOffset;

	public int Child3Index => Index + _child3IndexOffset;

	public int Child4Index => Index + _child4IndexOffset;

	public int Child5Index => Index + _child5IndexOffset;

	public int Child6Index => Index + _child6IndexOffset;

	public int Child7Index => Index + _child7IndexOffset;

	public bool IsValid => Index >= 0;

	public NodeStruct(ReadonlyVoxelOctree parent, int index, bool isLeaf, byte density, VoxelNodeType nodeType = VoxelNodeType.None, sbyte depth = -1, int child0IndexOffset = -1, int child1IndexOffset = -1, int child2IndexOffset = -1, int child3IndexOffset = -1, int child4IndexOffset = -1, int child5IndexOffset = -1, int child6IndexOffset = -1, int child7IndexOffset = -1, int parentIndex = -1)
	{
		Parent = parent;
		Index = index;
		IsLeaf = isLeaf;
		Depth = depth;
		Density = density;
		NodeType = nodeType;
		_child0IndexOffset = child0IndexOffset;
		_child1IndexOffset = child1IndexOffset;
		_child2IndexOffset = child2IndexOffset;
		_child3IndexOffset = child3IndexOffset;
		_child4IndexOffset = child4IndexOffset;
		_child5IndexOffset = child5IndexOffset;
		_child6IndexOffset = child6IndexOffset;
		_child7IndexOffset = child7IndexOffset;
		_parentIndex = parentIndex;
	}

	public NodeStruct GetChild(int childIndex)
	{
		if (IsLeaf)
		{
			return Invalid;
		}
		return childIndex switch
		{
			0 => Parent.GetNode(Child0Index, (sbyte)(Depth + 1), Index), 
			1 => Parent.GetNode(Child1Index, (sbyte)(Depth + 1), Index), 
			2 => Parent.GetNode(Child2Index, (sbyte)(Depth + 1), Index), 
			3 => Parent.GetNode(Child3Index, (sbyte)(Depth + 1), Index), 
			4 => Parent.GetNode(Child4Index, (sbyte)(Depth + 1), Index), 
			5 => Parent.GetNode(Child5Index, (sbyte)(Depth + 1), Index), 
			6 => Parent.GetNode(Child6Index, (sbyte)(Depth + 1), Index), 
			7 => Parent.GetNode(Child7Index, (sbyte)(Depth + 1), Index), 
			_ => throw new IndexOutOfRangeException(), 
		};
	}

	public int GetSize()
	{
		return (int)Mathf.Pow(2f, Mathf.Min(VoxelTerrain.MaxDepth, 10) - Depth);
	}

	public void DrawDebug()
	{
	}

	public void DrawChild(Vector3Int parentNodePosition, int childIndex)
	{
		ImGuiExtensions.Rendering.RenderingColor = VoxelTerrain.GetDebugColor(NodeType);
		int size = GetSize();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		if ((childIndex & 1) != 0)
		{
			num = 1;
		}
		if ((childIndex & 2) != 0)
		{
			num2 = 1;
		}
		if ((childIndex & 4) != 0)
		{
			num3 = 1;
		}
		Vector3Int vector3Int = parentNodePosition + new Vector3Int(num * size, num2 * size, num3 * size);
		Vector3 vector = Vector3.one * size;
		ImGuiExtensions.Rendering.DrawCube(vector3Int + vector * 0.5f, Vector3.one);
		if (!IsLeaf)
		{
			for (int i = 0; i < 8; i++)
			{
				GetChild(i).DrawChild(vector3Int, i);
			}
		}
	}

	public NodeInfo Info()
	{
		return new NodeInfo(Density, NodeType, Depth);
	}
}
