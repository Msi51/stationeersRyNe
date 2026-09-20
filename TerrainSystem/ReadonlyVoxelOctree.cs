using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Assets.Scripts;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace TerrainSystem;

public class ReadonlyVoxelOctree : IOctTree
{
	public const int SIZE_OF_BYTE = 1;

	public const int SIZE_OF_UINT16 = 2;

	public const int SIZE_OF_INT32 = 4;

	public const int SIZE_OF_FLAG = 1;

	public const int SIZE_OF_DENSITY = 1;

	public const int SIZE_OF_NODE_TYPE = 1;

	public const int FLAG_OFFSET = 0;

	public const int DENSITY_OFFSET = 1;

	public const int NODE_TYPE_OFFSET = 2;

	public const int CHILD_INDICES_OFFSET = 3;

	public const int SIZE_OF_LEAF = 3;

	[NativeDisableContainerSafetyRestriction]
	private NativeArray<byte> _data;

	public int Index { get; }

	public int MaxDepth { get; set; }

	public int CheckSum { get; set; }

	public ReadonlyVoxelOctree(int index)
	{
		Index = index;
	}

	public byte GetRootDensity()
	{
		return RootNode().Density;
	}

	public NodeInfo GetRootInfo()
	{
		return RootNode().Info();
	}

	public void Clear()
	{
		if (_data.IsCreated)
		{
			_data.Dispose();
		}
	}

	public static byte GetSizeFlag(int depth, int maxDepth, out int childOffsetSize)
	{
		switch (maxDepth - depth)
		{
		case 0:
			childOffsetSize = 0;
			return 0;
		case 1:
			childOffsetSize = 0;
			return 4;
		case 2:
		case 3:
		case 4:
			childOffsetSize = 2;
			return 16;
		default:
			childOffsetSize = 4;
			return 32;
		}
	}

	public static int GetChildOffsetIndexSize(byte nodeFlag)
	{
		if ((nodeFlag & 4) != 0)
		{
			return 0;
		}
		if ((nodeFlag & 8) != 0)
		{
			return 1;
		}
		if ((nodeFlag & 0x10) != 0)
		{
			return 2;
		}
		if ((nodeFlag & 0x20) != 0)
		{
			return 4;
		}
		throw new IndexOutOfRangeException("Child offset flag not set");
	}

	private void LoadEmptyChunk()
	{
		_data = new NativeArray<byte>(3, Allocator.Persistent);
		int index = 0;
		WriteByte(1, _data, ref index);
		WriteByte(0, _data, ref index);
		WriteByte(0, _data, ref index);
	}

	public void Deserialize(string filePath, NativeArray<byte> workingArray)
	{
		if (!File.Exists(filePath))
		{
			LoadEmptyChunk();
			return;
		}
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		using FileStream stream = new FileStream(filePath, FileMode.Open);
		using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress))
		{
			using MemoryStream memoryStream = new MemoryStream();
			gZipStream.CopyTo(memoryStream);
			gZipStream.Close();
			memoryStream.Seek(0L, SeekOrigin.Begin);
			using BinaryReader binaryReader = new BinaryReader(memoryStream);
			MaxDepth = (sbyte)binaryReader.ReadInt32();
			int depth = -1;
			int index = 0;
			DeserializeNode(binaryReader, workingArray, ref depth, ref index, out var _);
			CheckSum = binaryReader.ReadInt32();
			int length = index + 1;
			_data = new NativeArray<byte>(length, Allocator.Persistent);
			NativeArray<byte>.Copy(workingArray, _data, length);
		}
		stopwatch.Stop();
		ConsoleWindow.Print($"Deserialised in {(float)stopwatch.ElapsedMilliseconds / 1000f} s. Size in memory {_data.Length >> 20}MB ");
	}

	private void DeserializeNode(BinaryReader reader, NativeArray<byte> data, ref int depth, ref int index, out int childIndex)
	{
		depth++;
		byte b = reader.ReadByte();
		bool num = (b & 1) != 0;
		int num2 = index;
		int childOffsetSize = -1;
		if (!num)
		{
			b |= GetSizeFlag(depth, MaxDepth, out childOffsetSize);
		}
		WriteByte(b, data, ref index);
		if (num)
		{
			byte value = reader.ReadByte();
			byte value2 = reader.ReadByte();
			WriteByte(value, data, ref index);
			WriteByte(value2, data, ref index);
			childIndex = num2;
			depth--;
			return;
		}
		int num3 = index;
		WriteByte(0, data, ref index);
		WriteByte(0, data, ref index);
		int num4 = index;
		if (childOffsetSize > 0)
		{
			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < childOffsetSize; j++)
				{
					WriteByte(0, data, ref index);
				}
			}
		}
		for (int k = 0; k < 8; k++)
		{
			DeserializeNode(reader, data, ref depth, ref index, out childIndex);
			if (childOffsetSize > 0)
			{
				int num5 = index;
				index = num4 + k * childOffsetSize;
				switch (childOffsetSize)
				{
				case 1:
					WriteByte((byte)(childIndex - num2), data, ref index);
					break;
				case 2:
					WriteUInt16((ushort)(childIndex - num2), data, ref index);
					break;
				case 4:
					WriteInt32(childIndex - num2, data, ref index);
					break;
				default:
					throw new IndexOutOfRangeException("Out of range child offset size");
				case 0:
					break;
				}
				index = num5;
			}
		}
		int num6 = 0;
		int num7 = 0;
		for (int l = 0; l < 8; l++)
		{
			int index2 = num2 + 3 + l * childOffsetSize;
			int num8 = childOffsetSize switch
			{
				0 => 3 + l * 3, 
				1 => ReadByte(index2, data), 
				2 => ReadUInt16(index2, data), 
				4 => ReadInt32(index2, data), 
				_ => throw new IndexOutOfRangeException("Out of range child offset size"), 
			};
			num6 += ReadByte(num2 + num8 + 1, data);
			num7 |= ReadByte(num2 + num8 + 2, data);
		}
		int num9 = index;
		index = num3;
		WriteByte((byte)(num6 / 8), data, ref index);
		WriteByte((byte)num7, data, ref index);
		index = num9;
		childIndex = num2;
		depth--;
	}

	public bool GetIsLeaf(int nodeIndex)
	{
		return (ReadFlag(nodeIndex) & 1) != 0;
	}

	public byte Get(int nodeIndex)
	{
		return ReadDensity(nodeIndex);
	}

	public byte GetChildDensity(int parentNodeIndex, int childIndex)
	{
		return ReadDensity(GetChildNodeIndex(parentNodeIndex, childIndex));
	}

	public VoxelNodeType GetChildNodeType(int parentNodeIndex, int childIndex)
	{
		return ReadNodeType(GetChildNodeIndex(parentNodeIndex, childIndex));
	}

	public int GetChildNodeIndex(int parentNodeIndex, int childIndex)
	{
		int num = ReadChildIndexOffset(parentNodeIndex, childIndex, ChildOffsetSize(ReadFlag(parentNodeIndex)));
		return parentNodeIndex + num;
	}

	private byte ReadFlag(int index)
	{
		return ReadByte(index, _data);
	}

	private byte ReadDensity(int index)
	{
		return ReadByte(index + 1, _data);
	}

	private VoxelNodeType ReadNodeType(int index)
	{
		return (VoxelNodeType)ReadByte(index + 2, _data);
	}

	private int ReadChildIndexOffset(int index, int childIndex, int childOffsetSize = -1)
	{
		return childOffsetSize switch
		{
			0 => 3 + childIndex * 3, 
			1 => ReadByte(index + 3 + childIndex, _data), 
			2 => ReadUInt16(index + 3 + childIndex * 2, _data), 
			4 => ReadInt32(index + 3 + childIndex * 4, _data), 
			_ => throw new IndexOutOfRangeException("Invalid child offset size"), 
		};
	}

	private static byte ReadByte(int index, NativeArray<byte> data)
	{
		return data[index];
	}

	private static void WriteByte(byte value, NativeArray<byte> data, ref int index)
	{
		data[index] = value;
		index++;
	}

	private static ushort ReadUInt16(int index, NativeArray<byte> data)
	{
		return (ushort)(data[index] | (data[index + 1] << 8));
	}

	private static void WriteUInt16(ushort value, NativeArray<byte> data, ref int index)
	{
		data[index] = (byte)value;
		index++;
		data[index] = (byte)((uint)value >> 8);
		index++;
	}

	private static int ReadInt32(int index, NativeArray<byte> data)
	{
		return data[index] | (data[index + 1] << 8) | (data[index + 2] << 16) | (data[index + 3] << 24);
	}

	private static void WriteInt32(int value, NativeArray<byte> data, ref int index)
	{
		data[index] = (byte)value;
		index++;
		data[index] = (byte)(value >> 8);
		index++;
		data[index] = (byte)(value >> 16);
		index++;
		data[index] = (byte)(value >> 24);
		index++;
	}

	public NodeStruct GetNode(PartnerNode partnerNode, Vector3Int vector3Int, int desiredDepth)
	{
		sbyte depth = partnerNode.Depth;
		return GetNode(partnerNode.NodeIndex, vector3Int.x, vector3Int.y, vector3Int.z, ref depth, desiredDepth, partnerNode.NodeIndex);
	}

	public NodeInfo Get(Vector3Int vector3Int)
	{
		sbyte depth = 0;
		return Get(0, vector3Int.x, vector3Int.y, vector3Int.z, ref depth, MaxDepth);
	}

	public NodeInfo Get(Vector3Int vector3Int, int desiredDepth)
	{
		sbyte depth = 0;
		return Get(0, vector3Int.x, vector3Int.y, vector3Int.z, ref depth, desiredDepth);
	}

	public NodeInfo Get(int x, int y, int z, int desiredDepth)
	{
		sbyte depth = 0;
		return Get(0, x, y, z, ref depth, desiredDepth);
	}

	public NodeInfo Get(PartnerNode partnerNode, Vector3Int vector3Int, int desiredDepth)
	{
		sbyte depth = partnerNode.Depth;
		return Get(partnerNode.NodeIndex, vector3Int.x, vector3Int.y, vector3Int.z, ref depth, desiredDepth);
	}

	private NodeInfo Get(int nodeIndex, int x, int y, int z, ref sbyte depth, int desiredDepth)
	{
		if ((ReadFlag(nodeIndex) & 1) != 0 || depth == desiredDepth)
		{
			sbyte depth2 = ((VoxelTerrain.MaxDepth <= 10) ? depth : ((sbyte)(depth + (VoxelTerrain.MaxDepth - 10))));
			return new NodeInfo(ReadDensity(nodeIndex), ReadNodeType(nodeIndex), depth2);
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
		int num3 = ReadChildIndexOffset(nodeIndex, num2, GetChildOffsetIndexSize(ReadFlag(nodeIndex)));
		depth++;
		return Get(nodeIndex + num3, x, y, z, ref depth, desiredDepth);
	}

	private NodeStruct GetNode(int nodeIndex, int x, int y, int z, ref sbyte depth, int desiredDepth, int parentIndex)
	{
		if ((ReadFlag(nodeIndex) & 1) != 0 || depth == desiredDepth)
		{
			return GetNode(nodeIndex, depth, parentIndex);
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
		int num3 = ReadChildIndexOffset(nodeIndex, num2, GetChildOffsetIndexSize(ReadFlag(nodeIndex)));
		depth++;
		return GetNode(nodeIndex + num3, x, y, z, ref depth, desiredDepth, nodeIndex);
	}

	public INode GetRoot()
	{
		return RootNode();
	}

	public NodeStruct RootNode()
	{
		return GetNode(0, 0, -1);
	}

	public NodeStruct GetNode(int index, sbyte foundDepth, int parentIndex)
	{
		if (index < 0)
		{
			return NodeStruct.Invalid;
		}
		byte b = ReadFlag(index);
		bool flag = (b & 1) != 0;
		int childOffsetSize = ((!flag) ? ChildOffsetSize(b) : 0);
		return new NodeStruct(this, index, flag, ReadDensity(index), ReadNodeType(index), foundDepth, (!flag) ? ReadChildIndexOffset(index, 0, childOffsetSize) : 0, (!flag) ? ReadChildIndexOffset(index, 1, childOffsetSize) : 0, (!flag) ? ReadChildIndexOffset(index, 2, childOffsetSize) : 0, (!flag) ? ReadChildIndexOffset(index, 3, childOffsetSize) : 0, (!flag) ? ReadChildIndexOffset(index, 4, childOffsetSize) : 0, (!flag) ? ReadChildIndexOffset(index, 5, childOffsetSize) : 0, (!flag) ? ReadChildIndexOffset(index, 6, childOffsetSize) : 0, (!flag) ? ReadChildIndexOffset(index, 7, childOffsetSize) : 0, parentIndex);
	}

	public PartnerNode GetPartnerNode(int x, int y, int z, int desiredDepth)
	{
		return GetPartnerNode(0, x, y, z, desiredDepth, 0);
	}

	private PartnerNode GetPartnerNode(int nodeIndex, int x, int y, int z, int desiredDepth, sbyte depth)
	{
		bool flag = (ReadFlag(nodeIndex) & 1) != 0;
		if (depth == desiredDepth)
		{
			return new PartnerNode((ushort)Index, nodeIndex, depth);
		}
		if (flag)
		{
			return PartnerNode.Invalid;
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
		int num3 = ReadChildIndexOffset(nodeIndex, num2, GetChildOffsetIndexSize(ReadFlag(nodeIndex)));
		depth++;
		return GetPartnerNode(nodeIndex + num3, x, y, z, desiredDepth, (sbyte)(depth + 1));
	}

	public int GetSize(int depth)
	{
		return (int)Mathf.Pow(2f, Mathf.Min(VoxelTerrain.MaxDepth, 10) - depth);
	}

	public static sbyte GetDepth(int size)
	{
		return (sbyte)(VoxelTerrain.MaxDepth - (int)Mathf.Log(size, 2f));
	}

	public static int ChildOffsetSize(byte flags)
	{
		if ((flags & 4) != 0)
		{
			return 0;
		}
		if ((flags & 8) != 0)
		{
			return 1;
		}
		if ((flags & 0x10) != 0)
		{
			return 2;
		}
		if ((flags & 0x20) != 0)
		{
			return 4;
		}
		throw new IndexOutOfRangeException("Child offset not specified");
	}
}
