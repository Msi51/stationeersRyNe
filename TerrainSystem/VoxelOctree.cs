using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using UnityEngine;

namespace TerrainSystem;

[Serializable]
public class VoxelOctree : IOctTree
{
	public OctTreeCluster MasterTree;

	public Node Root;

	private int _maxDepth;

	public readonly bool IsMasterTree = true;

	public int MaxDepth
	{
		get
		{
			return _maxDepth;
		}
		set
		{
			_maxDepth = value;
			IOctTree maxDepth;
			if (!IsMasterTree)
			{
				IOctTree masterTree = MasterTree;
				maxDepth = masterTree;
			}
			else
			{
				IOctTree masterTree = this;
				maxDepth = masterTree;
			}
			VoxelTerrain.SetMaxDepth(maxDepth);
		}
	}

	public byte GetRootDensity()
	{
		return Root.Density;
	}

	public INode GetRoot()
	{
		return Root;
	}

	public VoxelOctree(int size)
	{
		MasterTree = null;
		Root = new Node
		{
			Density = 0,
			Depth = 0
		};
		IsMasterTree = true;
		MaxDepth = (sbyte)Mathf.Log(size, 2f);
	}

	public VoxelOctree(OctTreeCluster masterTree)
	{
		MasterTree = masterTree;
		if (MasterTree != null)
		{
			MaxDepth = masterTree.MaxDepth;
			Node root = new Node
			{
				Density = masterTree.GetRootDensity(),
				Depth = 0
			};
			Root = root;
			Root.SetPartnerNode();
		}
		else
		{
			Root = new Node
			{
				Density = 0,
				Depth = 0
			};
		}
		IsMasterTree = masterTree == null;
	}

	public void PrepareOctree()
	{
		int num = (int)Mathf.Pow(2f, MaxDepth);
		if (num > 1024)
		{
			PrepareNode(MasterTree, Root, num);
		}
	}

	private void PrepareNode(OctTreeCluster masterTree, Node node, int size)
	{
		if (size == 1024)
		{
			return;
		}
		node.Children = new Node[8];
		for (int i = 0; i < node.Children.Length; i++)
		{
			Node node2 = new Node
			{
				Parent = node,
				NodeType = VoxelNodeType.None,
				Depth = (byte)(node.Depth + 1)
			};
			node.Children[i] = node2;
			if (size / 2 == 1024)
			{
				if (IsMasterTree)
				{
					node2.Density = 0;
				}
				else
				{
					node2.SetPartnerNode();
					if (node2.PartnerNode.IsValid)
					{
						node2.Density = node2.PartnerNode.GetDensity();
					}
					else
					{
						node2.Density = masterTree.Get(node.GetVoxelPosition(), (sbyte)Mathf.Log(size, 2f)).Density;
					}
				}
			}
			PrepareNode(masterTree, node2, size / 2);
		}
	}

	public void SerializeDeltaTerrain(Stream memoryStream)
	{
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
		binaryWriter.Write(MaxDepth);
		int checksum = 0;
		SerializeNode(Root, binaryWriter, ref checksum);
		Vein.SerializeSave(binaryWriter);
	}

	public bool DeserializeDeltaTerrain(string filePath)
	{
		if (!File.Exists(filePath))
		{
			ConsoleWindow.PrintError("File not found: " + filePath);
			return false;
		}
		try
		{
			int count = 0;
			using (FileStream input = new FileStream(filePath, FileMode.Open))
			{
				using BinaryReader binaryReader = new BinaryReader(input);
				MaxDepth = binaryReader.ReadInt32();
				int depth = -1;
				Root = DeserializeNode(binaryReader, ref count, ref depth);
				PartnerNode partnerNode = VoxelTerrain.GetPartnerNode(Root);
				SetPartnerNode(Root, partnerNode);
				Vein.DeserializeSave(binaryReader);
			}
			ConsoleWindow.Print($"Deserialised {count} nodes");
		}
		catch (Exception ex)
		{
			ConsoleWindow.PrintError("Failed to deserialize terrain from " + filePath + ": " + ex.Message);
			return false;
		}
		return true;
	}

	public void SetPartnerNode(Node node, PartnerNode partnerNode)
	{
		if (!partnerNode.IsValid)
		{
			partnerNode = VoxelTerrain.GetPartnerNode(node);
		}
		if (!node.IsModified)
		{
			node.PartnerNode = partnerNode;
		}
		if (!node.IsLeaf && (!partnerNode.IsValid || !partnerNode.IsLeaf()))
		{
			for (int i = 0; i < node.Children.Length; i++)
			{
				SetPartnerNode(node.Children[i], partnerNode.GetChildPartnerNode(i));
			}
		}
	}

	public void Serialize(string folderPath)
	{
		int octreeCount = OctTreeCluster.GetOctreeCount(MaxDepth);
		int newRootDepth = Mathf.Max(0, MaxDepth - 10);
		List<Node> serializeNodes = new List<Node>(octreeCount);
		int depth = -1;
		GetNewRootNodesForSerialise(Root, serializeNodes, ref depth, newRootDepth);
		if (serializeNodes.Count != octreeCount)
		{
			throw new Exception("Serialization failed");
		}
		int maxClusterDepth = Mathf.Max(0, MaxDepth - 10);
		Parallel.For(0, octreeCount, delegate(int i)
		{
			if (serializeNodes[i].GetVoxelPosition().y > 1023)
			{
				return;
			}
			using MemoryStream memoryStream = new MemoryStream();
			using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			int checksum = 0;
			binaryWriter.Write(MaxDepth - maxClusterDepth);
			SerializeNode(serializeNodes[i], binaryWriter, ref checksum);
			binaryWriter.Write(checksum);
			using MemoryStream memoryStream2 = new MemoryStream();
			using GZipStream gZipStream = new GZipStream(memoryStream2, System.IO.Compression.CompressionLevel.Optimal);
			byte[] array = memoryStream.ToArray();
			gZipStream.Write(array);
			gZipStream.Close();
			using FileStream output = new FileStream($"{folderPath}/Terrain{i}.dat", FileMode.Create);
			using BinaryWriter binaryWriter2 = new BinaryWriter(output);
			binaryWriter2.Write(memoryStream2.ToArray());
		});
	}

	private void GetNewRootNodesForSerialise(Node node, List<Node> nodes, ref int depth, int newRootDepth)
	{
		depth++;
		if (depth == newRootDepth)
		{
			nodes.Add(node);
			depth--;
			return;
		}
		Node[] children = node.Children;
		foreach (Node node2 in children)
		{
			GetNewRootNodesForSerialise(node2, nodes, ref depth, newRootDepth);
		}
		depth--;
	}

	public void SerializeOnJoin(RocketBinaryWriter writer)
	{
		writer.WriteInt32(MaxDepth);
		WriteNode(Root, writer);
	}

	public void DeserializeOnJoin(RocketBinaryReader reader)
	{
		MaxDepth = reader.ReadInt32();
		int depth = -1;
		Root = ReadNode(reader, ref depth, null);
		PartnerNode partnerNode = VoxelTerrain.GetPartnerNode(Root);
		SetPartnerNode(Root, partnerNode);
	}

	protected void SerializeNode(Node node, BinaryWriter writer, ref int checksum)
	{
		byte b = 0;
		if (node.IsLeaf)
		{
			b |= 1;
		}
		if (node.IsModified)
		{
			b |= 2;
		}
		checksum = (checksum ^ b) * 257;
		writer.Write(b);
		if (node.IsLeaf)
		{
			writer.Write(node.Density);
			writer.Write((byte)node.NodeType);
			checksum = (checksum ^ node.Density) * 257;
			checksum = (int)(((uint)checksum ^ (uint)node.NodeType) * 257);
		}
		else
		{
			for (int i = 0; i < 8; i++)
			{
				checksum = (checksum ^ i) * 257;
				SerializeNode(node.Children[i], writer, ref checksum);
			}
		}
	}

	protected Node DeserializeNode(BinaryReader reader, ref int count, ref int depth)
	{
		depth++;
		byte num = reader.ReadByte();
		bool flag = (num & 1) != 0;
		bool isModified = (num & 2) != 0;
		Node node = new Node
		{
			Children = null,
			Depth = (byte)depth,
			IsModified = isModified
		};
		count++;
		if (flag)
		{
			node.Density = reader.ReadByte();
			node.NodeType = (VoxelNodeType)reader.ReadByte();
			depth--;
			return node;
		}
		node.Children = new Node[8];
		for (int i = 0; i < 8; i++)
		{
			Node node2 = DeserializeNode(reader, ref count, ref depth);
			node.Children[i] = node2;
			node.Children[i].Parent = node;
		}
		depth--;
		return node;
	}

	protected void WriteNode(Node node, RocketBinaryWriter writer)
	{
		byte b = 0;
		if (node.IsLeaf)
		{
			b |= 1;
		}
		if (node.IsModified)
		{
			b |= 2;
		}
		writer.WriteByte(b);
		if (node.IsLeaf)
		{
			writer.WriteByte(node.Density);
			return;
		}
		for (int i = 0; i < 8; i++)
		{
			WriteNode(node.Children[i], writer);
		}
	}

	protected Node ReadNode(RocketBinaryReader reader, ref int depth, Node parent)
	{
		depth++;
		byte num = reader.ReadByte();
		bool flag = (num & 1) != 0;
		bool isModified = (num & 2) != 0;
		Node node = new Node
		{
			Children = null,
			IsModified = isModified,
			Depth = (byte)depth,
			Parent = parent
		};
		if (flag)
		{
			node.Density = reader.ReadByte();
			depth--;
			return node;
		}
		node.Children = new Node[8];
		for (int i = 0; i < 8; i++)
		{
			Node node2 = ReadNode(reader, ref depth, node);
			node.Children[i] = node2;
		}
		depth--;
		return node;
	}

	public int GetVoxelCountOneDimension()
	{
		return 1 << MaxDepth;
	}

	public bool OutsideBounds(Vector3Int octreePosition)
	{
		if (octreePosition.x >= 0 && octreePosition.y >= 0 && octreePosition.z >= 0 && octreePosition.x < VoxelConstants.Size && octreePosition.y < VoxelConstants.Size)
		{
			return octreePosition.z >= VoxelConstants.Size;
		}
		return true;
	}

	private bool InsideBounds(Vector3Int position, Vector3Int nodePosition, int nodeSize)
	{
		if (position.x >= nodePosition.x && position.x < nodePosition.x + nodeSize && position.y >= nodePosition.y && position.y < nodePosition.y + nodeSize && position.z >= nodePosition.z)
		{
			return position.z < nodePosition.z + nodeSize;
		}
		return false;
	}

	public Node GetNodeAtLocationWithSize(Vector3Int position, int size)
	{
		return GetNodeAtLocationWithSize(Root, position, size, 0);
	}

	private Node GetNodeAtLocationWithSize(Node node, Vector3Int position, int size, int depth)
	{
		if (node == null)
		{
			return null;
		}
		Vector3Int voxelPosition = node.GetVoxelPosition();
		int size2 = node.GetSize();
		if (!InsideBounds(position, voxelPosition, size2))
		{
			return null;
		}
		if (size2 == size)
		{
			return node;
		}
		if (node.IsLeaf || depth == MaxDepth)
		{
			return null;
		}
		for (int i = 0; i < 8; i++)
		{
			Node nodeAtLocationWithSize = GetNodeAtLocationWithSize(node.Children[i], position, size, depth + 1);
			if (nodeAtLocationWithSize != null)
			{
				return nodeAtLocationWithSize;
			}
		}
		return null;
	}

	public NodeInfo Get(Vector3 position)
	{
		Node foundNode;
		return Get(position, out foundNode);
	}

	public NodeInfo Get(Vector3 position, out Node foundNode)
	{
		Vector3Int foundPosition;
		return Get(position.FloorToInt(), out foundNode, out foundPosition);
	}

	public NodeInfo Get(Vector3Int position, out Node foundNode, out Vector3Int foundPosition)
	{
		return Get(Root, position.x, position.y, position.z, 0, out foundNode, out foundPosition, MaxDepth);
	}

	public NodeInfo Get(int x, int y, int z, out Node foundNode, out Vector3Int foundPos)
	{
		return Get(Root, x, y, z, 0, out foundNode, out foundPos, MaxDepth);
	}

	public NodeInfo Get(Vector3Int position, out Node foundNode, int desiredDepth)
	{
		Vector3Int foundNodePosition;
		return Get(Root, position.x, position.y, position.z, 0, out foundNode, out foundNodePosition, desiredDepth);
	}

	public NodeInfo Get(int x, int y, int z, out Node foundNode, out Vector3Int foundPos, int desiredDepth)
	{
		return Get(Root, x, y, z, 0, out foundNode, out foundPos, desiredDepth);
	}

	private NodeInfo Get(Node node, int x, int y, int z, sbyte depth, out Node foundNode, out Vector3Int foundNodePosition, int desiredDepth)
	{
		foundNode = null;
		if (node == null)
		{
			foundNodePosition = Vector3Int.zero;
			return NodeInfo.Invalid;
		}
		if (depth == desiredDepth || node.IsLeaf)
		{
			foundNode = node;
			foundNodePosition = new Vector3Int(x, y, z);
			return new NodeInfo(node.Density, node.NodeType, depth);
		}
		int num = 1 << MaxDepth - depth - 1;
		int num2 = 0;
		if (x >= num)
		{
			num2 |= 1;
			x -= num;
		}
		if (y >= num)
		{
			num2 |= 2;
			y -= num;
		}
		if (z >= num)
		{
			num2 |= 4;
			z -= num;
		}
		return Get(node.Children[num2], x, y, z, (sbyte)(depth + 1), out foundNode, out foundNodePosition, desiredDepth);
	}

	public void SetDensity(Vector3 position, byte density, bool setNodeType = false, VoxelNodeType nodeType = VoxelNodeType.None)
	{
		if (!(position.x < 0f) && !(position.y < 0f) && !(position.z < 0f))
		{
			int x = (int)position.x;
			int y = (int)position.y;
			int z = (int)position.z;
			SetDensity(Root, x, y, z, 0, density, setNodeType, nodeType, IsMasterTree);
		}
	}

	private void SetDensity(Node node, int x, int y, int z, int depth, byte density, bool setNodeType, VoxelNodeType nodeType, bool isMasterTree)
	{
		if (depth == MaxDepth)
		{
			node.Density = density;
			if (density == 0)
			{
				node.NodeType = VoxelNodeType.None;
			}
			else if (setNodeType)
			{
				node.NodeType = nodeType;
			}
			node.OnSetDensity();
			node.CollapseUp();
			return;
		}
		if (node.IsLeaf)
		{
			if (!node.IsSubdivideOnSetDensity(density, nodeType))
			{
				return;
			}
			node.Subdivide(isMasterTree);
		}
		int num = 1 << MaxDepth - depth - 1;
		int num2 = 0;
		if (x >= num)
		{
			num2 |= 1;
			x -= num;
		}
		if (y >= num)
		{
			num2 |= 2;
			y -= num;
		}
		if (z >= num)
		{
			num2 |= 4;
			z -= num;
		}
		SetDensity(node.Children[num2], x, y, z, depth + 1, density, setNodeType, nodeType, isMasterTree);
	}

	public void CopyAll()
	{
		CopyFromReadOnly(Root);
	}

	public void CopyArea(Vector3Int position, int copySize)
	{
		int num;
		for (num = 1; num < copySize; num *= 2)
		{
		}
		Node nodeAtLocationWithSize = GetNodeAtLocationWithSize(position, num);
		while (nodeAtLocationWithSize == null && num > 1)
		{
			num /= 2;
			nodeAtLocationWithSize = GetNodeAtLocationWithSize(position, num);
		}
		if (nodeAtLocationWithSize != null)
		{
			CopyFromReadOnly(nodeAtLocationWithSize);
		}
	}

	private void CopyFromReadOnly(Node startNode)
	{
		Copy(startNode);
	}

	private void Copy(Node node)
	{
		if (!node.IsLeaf)
		{
			for (int i = 0; i < node.Children.Length; i++)
			{
				Copy(node.Children[i]);
			}
		}
		else if (!node.IsModified && node.PartnerNode.IsValid && !node.PartnerNode.IsLeaf())
		{
			node.Subdivide(IsMasterTree);
			for (int j = 0; j < node.Children.Length; j++)
			{
				Copy(node.Children[j]);
			}
		}
		if (node.IsLeaf)
		{
			node.IsModified = true;
		}
	}

	private void ResizeOctree()
	{
		Node node = new Node
		{
			Depth = 0
		};
		node.Subdivide(IsMasterTree);
		Root.Parent = node;
		node.Children[0] = Root;
		Root = node;
		MaxDepth++;
		int depth = -1;
		UpdateNodeDepth(Root, ref depth);
	}

	private static void UpdateNodeDepth(Node current, ref int depth)
	{
		depth++;
		current.Depth = (byte)depth;
		if (current.IsLeaf)
		{
			depth--;
			return;
		}
		for (int i = 0; i < 8; i++)
		{
			UpdateNodeDepth(current.Children[i], ref depth);
		}
		depth--;
	}

	public void Clear()
	{
		if (Root != null)
		{
			Root.Children = null;
			Root.Density = 0;
		}
		MaxDepth = 0;
		MasterTree = null;
	}

	public void DebugPrintVoxels()
	{
		DebugPrintVoxels(Root, new Vector3Int(0, 0, 0), MaxDepth);
	}

	private void DebugPrintVoxels(Node node, Vector3Int position, int depth)
	{
		if (node == null)
		{
			return;
		}
		if (depth == 0 && node.IsLeaf && node.Density > 0)
		{
			Debug.Log($"Voxel Position: {position}, Density: {node.Density}");
		}
		else if (!node.IsLeaf)
		{
			int num = 1 << depth - 1;
			for (int i = 0; i < 8; i++)
			{
				int num2 = (((i & 1) == 1) ? num : 0);
				int num3 = (((i & 2) == 2) ? num : 0);
				int num4 = (((i & 4) == 4) ? num : 0);
				DebugPrintVoxels(position: new Vector3Int(position.x + num2, position.y + num3, position.z + num4), node: node.Children[i], depth: depth - 1);
			}
		}
	}
}
