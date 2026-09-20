using System;
using Assets.Scripts;
using Assets.Scripts.UI.ImGuiUi;
using UnityEngine;

namespace TerrainSystem;

[Serializable]
public class Node : INode
{
	public const byte DENSITY_THRESHOLD = 1;

	private byte _density;

	protected VoxelNodeType _nodeType;

	private bool _isModified;

	public Node[] Children;

	public Node Parent;

	public PartnerNode PartnerNode = PartnerNode.Invalid;

	public const int CHECK_SUM_PRIME = 257;

	public byte Density
	{
		get
		{
			if (Children == null)
			{
				return _density;
			}
			int num = 0;
			Node[] children = Children;
			foreach (Node node in children)
			{
				if (node == null)
				{
					return _density;
				}
				num += node.Density;
			}
			num /= 8;
			return (byte)num;
		}
		set
		{
			_density = value;
		}
	}

	public VoxelNodeType NodeType
	{
		get
		{
			if (Children == null)
			{
				return _nodeType;
			}
			VoxelNodeType voxelNodeType = VoxelNodeType.None;
			Node[] children = Children;
			foreach (Node node in children)
			{
				if (node == null)
				{
					return _nodeType;
				}
				voxelNodeType |= node.NodeType;
			}
			return voxelNodeType;
		}
		set
		{
			_nodeType = value;
		}
	}

	public bool IsValid => true;

	public bool IsLeaf => Children == null;

	public bool IsModified
	{
		get
		{
			return _isModified;
		}
		set
		{
			_isModified = value;
			if (value && Parent != null)
			{
				Parent.IsModified = true;
			}
		}
	}

	public byte Depth { get; set; }

	public int GetSize()
	{
		return (int)Mathf.Pow(2f, VoxelTerrain.MaxDepth - Depth);
	}

	public void DrawDebug()
	{
		DrawChild(GetVoxelPosition(), 0);
	}

	public void DrawChild(Vector3Int parentNodePosition, int childIndex)
	{
		ImGuiExtensions.Rendering.RenderingColor = VoxelTerrain.GetDebugColor(NodeType);
		int size = GetSize();
		Vector3 vector = Vector3.one * size;
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
		ImGuiExtensions.Rendering.DrawCube(VoxelTerrain.OctreeToWorldSpace(vector3Int + vector * 0.5f, VoxelConstants.OriginOffsetInt), vector);
		if (IsLeaf)
		{
			if (!IsModified && PartnerNode.IsValid)
			{
				PartnerNode.DrawChildren(vector3Int);
			}
		}
		else
		{
			for (int i = 0; i < Children.Length; i++)
			{
				Children[i].DrawChild(vector3Int, i);
			}
		}
	}

	public void SetPartnerNode()
	{
		PartnerNode = VoxelTerrain.GetPartnerNode(this);
	}

	public Vector3Int GetVoxelPosition()
	{
		Vector3Int result = new Vector3Int(0, 0, 0);
		Node node = this;
		while (node.Parent != null)
		{
			int num = ((node.Parent.Children != null) ? Array.IndexOf(node.Parent.Children, node) : 0);
			if (num < 0)
			{
				ConsoleWindow.PrintError("Child not found in parent's children array");
				return new Vector3Int(-1, -1, -1);
			}
			int size = node.GetSize();
			int x = (num & 1) * size;
			int y = ((num >> 1) & 1) * size;
			int z = ((num >> 2) & 1) * size;
			result += new Vector3Int(x, y, z);
			node = node.Parent;
		}
		return result;
	}

	public void Subdivide(bool isMasterTree)
	{
		byte density = Density;
		VoxelNodeType nodeType = NodeType;
		Children = new Node[8];
		byte depth = (byte)(Depth + 1);
		for (int i = 0; i < 8; i++)
		{
			Node node = new Node
			{
				Depth = depth,
				Parent = this,
				NodeType = _nodeType
			};
			if (!IsModified && !isMasterTree)
			{
				if (!PartnerNode.IsLeaf())
				{
					node.Density = PartnerNode.GetChildDensity(i);
					node._nodeType = PartnerNode.GetChildNodeType(i);
					node.PartnerNode = PartnerNode.GetChildPartnerNode(i);
				}
				else
				{
					node.Density = density;
					node.NodeType = nodeType;
					node.IsModified = true;
				}
			}
			else
			{
				node.Density = density;
				node.IsModified = true;
				node.NodeType = nodeType;
			}
			Children[i] = node;
		}
	}

	public void CollapseUp()
	{
		Node node = this;
		while (node.Parent != null && node.GetSize() < 1024)
		{
			Node parent = node.Parent;
			if (!parent.AreChildrenCollapsible())
			{
				break;
			}
			int num = 0;
			bool isModified = false;
			for (int i = 0; i < 8; i++)
			{
				num += parent.Children[i].Density;
				if (parent.Children[i].IsModified)
				{
					isModified = true;
				}
			}
			num /= 8;
			parent.Density = (byte)num;
			parent.Children = null;
			parent.NodeType = node.NodeType;
			parent.IsModified = isModified;
			node = parent;
		}
	}

	protected bool AreChildrenCollapsible()
	{
		if (Children == null)
		{
			return false;
		}
		if (AreChildrenSameNodeType(this))
		{
			return AreChildrenDensitiesClose(this, (int)AverageDensity(this));
		}
		return false;
	}

	private static bool AreChildrenDensitiesClose(Node node, float averageDensity)
	{
		for (int i = 0; i < 8; i++)
		{
			if (Mathf.Abs((float)(int)node.Children[i].Density - averageDensity) >= 1f)
			{
				return false;
			}
		}
		return true;
	}

	private static byte AverageDensity(Node node)
	{
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			num += node.Children[i].Density;
		}
		num /= 8;
		return (byte)num;
	}

	private static bool AreChildrenSameNodeType(Node node)
	{
		VoxelNodeType nodeType = node.Children[0].NodeType;
		for (int i = 1; i < 8; i++)
		{
			if (node.Children[i].NodeType != nodeType)
			{
				return false;
			}
		}
		return true;
	}

	public virtual void OnSetDensity()
	{
		IsModified = true;
	}

	public bool IsSubdivideOnSetDensity(byte newDensity, VoxelNodeType nodeType)
	{
		if (Mathf.Abs(Density - newDensity) > 1 || (nodeType & NodeType) != VoxelNodeType.None)
		{
			return true;
		}
		return false;
	}

	public Node GetLower()
	{
		if (Parent != null)
		{
			return Array.IndexOf(Parent.Children, this) switch
			{
				2 => Parent.Children[0], 
				3 => Parent.Children[1], 
				6 => Parent.Children[4], 
				7 => Parent.Children[5], 
				_ => null, 
			};
		}
		return null;
	}

	public bool GetChildrenFacing(Vector3Int direction, Node[] children)
	{
		if (direction == Vector3Int.up)
		{
			return Set(children, 2, 3, 6, 7);
		}
		if (direction == Vector3Int.down)
		{
			return Set(children, 0, 1, 4, 5);
		}
		if (direction == Vector3Int.left)
		{
			return Set(children, 0, 2, 4, 6);
		}
		if (direction == Vector3Int.right)
		{
			return Set(children, 1, 3, 5, 7);
		}
		if (direction == Vector3Int.forward)
		{
			return Set(children, 4, 5, 6, 7);
		}
		if (direction == Vector3Int.back)
		{
			return Set(children, 0, 1, 2, 3);
		}
		return false;
		bool Set(Node[] array, int i0, int i1, int i2, int i3)
		{
			if (Children == null)
			{
				return false;
			}
			array[0] = Children[i0];
			array[1] = Children[i1];
			array[2] = Children[i2];
			array[3] = Children[i3];
			return true;
		}
	}
}
