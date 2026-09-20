using System.Collections.Generic;
using Assets.Scripts.Util;
using UnityEngine;

namespace TerrainSystem;

public class MarchingCubes
{
	protected float uvScale = 1f;

	public byte Surface { get; set; }

	private byte[] Cube { get; } = new byte[8];

	protected int[] WindingOrder { get; } = new int[3] { 0, 1, 2 };

	private Vector3[] EdgeVertex { get; } = new Vector3[12];

	private byte[] CubeValue { get; } = new byte[8];

	public void Generate(List<Vector3> verts, List<Vector3> normals, List<Vector3> deformationNormals, List<byte> nodeTypes, List<Vector2> minableColor, List<int> indices, List<Vein> veins, int offsetX, int offsetY, int offsetZ, int chunkSize, sbyte desiredDepth)
	{
		UpdateWindingOrder();
		int num = (int)Mathf.Pow(2f, VoxelTerrain.MaxDepth - desiredDepth);
		Vector3Int vector3Int = new Vector3Int(offsetX * chunkSize, offsetY * chunkSize, offsetZ * chunkSize);
		if (desiredDepth == VoxelTerrain.MaxDepth)
		{
			veins.Clear();
			veins.Capacity = 64;
			Vein.GetVeinsOctreeSpace(vector3Int, new Vector3Int(vector3Int.x + chunkSize, vector3Int.y + chunkSize, vector3Int.z + chunkSize), ref veins);
		}
		for (int i = 0; i < chunkSize; i += num)
		{
			for (int j = 0; j < chunkSize; j += num)
			{
				for (int k = 0; k < chunkSize; k += num)
				{
					VoxelNodeType voxelNodeType = VoxelNodeType.None;
					for (int l = 0; l < 8; l++)
					{
						int num2 = i + MarchingConstants.VertexOffset[l, 0] * num;
						int num3 = j + MarchingConstants.VertexOffset[l, 1] * num;
						int num4 = k + MarchingConstants.VertexOffset[l, 2] * num;
						NodeInfo nodeInfo = VoxelTerrain.Get(num2 + offsetX * chunkSize, num3 + offsetY * chunkSize, num4 + offsetZ * chunkSize, desiredDepth);
						Cube[l] = nodeInfo.Density;
						voxelNodeType |= nodeInfo.NodeType;
					}
					VoxelNodeType voxelNodeType2 = VoxelNodeType.None;
					if ((voxelNodeType & VoxelNodeType.Macro) != VoxelNodeType.None)
					{
						voxelNodeType2 = VoxelNodeType.Macro;
					}
					else if ((voxelNodeType & VoxelNodeType.Crust) != VoxelNodeType.None)
					{
						voxelNodeType2 |= VoxelNodeType.Crust;
					}
					else if ((voxelNodeType & VoxelNodeType.Dirt) != VoxelNodeType.None)
					{
						voxelNodeType2 |= VoxelNodeType.Dirt;
					}
					March(i, j, k, Cube, verts, normals, deformationNormals, indices, nodeTypes, minableColor, veins, num, vector3Int, desiredDepth, voxelNodeType2);
				}
			}
		}
	}

	private void UpdateWindingOrder()
	{
		if ((float)(int)Surface < 0f)
		{
			WindingOrder[0] = 2;
			WindingOrder[1] = 1;
			WindingOrder[2] = 0;
		}
		else
		{
			WindingOrder[0] = 0;
			WindingOrder[1] = 1;
			WindingOrder[2] = 2;
		}
	}

	private void March(float x, float y, float z, byte[] cube, List<Vector3> vertList, List<Vector3> normalList, List<Vector3> deformationNormalList, List<int> indexList, List<byte> nodeTypes, List<Vector2> minableColor, List<Vein> veins, float voxelSize, Vector3 chunkPosition, sbyte desiredDepth, VoxelNodeType fallbackNodeType)
	{
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			CubeValue[i] = cube[i];
			if (CubeValue[i] <= Surface)
			{
				num |= 1 << i;
			}
		}
		int num2 = MarchingConstants.CUBE_EDGE_FLAGS[num];
		if (num2 == 0)
		{
			return;
		}
		if (vertList.Count == 0)
		{
			vertList.Capacity = 800;
			normalList.Capacity = 800;
			deformationNormalList.Capacity = 800;
			indexList.Capacity = 800;
			nodeTypes.Capacity = 800;
			minableColor.Capacity = 800;
		}
		for (int i = 0; i < 12; i++)
		{
			if ((num2 & (1 << i)) != 0)
			{
				int num3 = MarchingConstants.EDGE_CONNECTION[i, 0];
				int num4 = MarchingConstants.EDGE_CONNECTION[i, 1];
				float offset = GetOffset(CubeValue[num3], CubeValue[num4]);
				float num5 = 1f - offset;
				EdgeVertex[i].x = num5 * (x + (float)MarchingConstants.VertexOffset[num3, 0] * voxelSize) + offset * (x + (float)MarchingConstants.VertexOffset[num4, 0] * voxelSize);
				EdgeVertex[i].y = num5 * (y + (float)MarchingConstants.VertexOffset[num3, 1] * voxelSize) + offset * (y + (float)MarchingConstants.VertexOffset[num4, 1] * voxelSize);
				EdgeVertex[i].z = num5 * (z + (float)MarchingConstants.VertexOffset[num3, 2] * voxelSize) + offset * (z + (float)MarchingConstants.VertexOffset[num4, 2] * voxelSize);
			}
		}
		for (int i = 0; i < 5 && MarchingConstants.TRIANGLE_CONNECTION_TABLE[num][3 * i] >= 0; i++)
		{
			Vector3[] array = new Vector3[3];
			for (int j = 0; j < 3; j++)
			{
				int num6 = MarchingConstants.TRIANGLE_CONNECTION_TABLE[num][3 * i + j];
				array[j] = EdgeVertex[num6];
			}
			if (!ValidateTriangle(array, out var normal))
			{
				continue;
			}
			int count = vertList.Count;
			for (int k = 0; k < 3; k++)
			{
				Vector3 vector = ComputeNormal(chunkPosition + array[k], voxelSize, desiredDepth);
				float num7 = Vector3.Dot(vector, normal);
				Vector3 vector2 = ((desiredDepth != VoxelTerrain.MaxDepth) ? vector : ((num7 < 0.8f) ? normal : vector));
				vertList.Add(array[k]);
				normalList.Add(vector2.normalized);
				deformationNormalList.Add(vector.normalized);
				indexList.Add(count + WindingOrder[k]);
				VoxelNodeType voxelNodeType;
				if (desiredDepth >= VoxelTerrain.MaxDepth - 1)
				{
					Vector3 vector3 = deformationNormalList[deformationNormalList.Count - 1];
					Vector3 vector4 = vertList[vertList.Count - 1];
					Vector3 vector5 = new Vector3(vector4.x + chunkPosition.x, vector4.y + chunkPosition.y, vector4.z + chunkPosition.z);
					float f = vector5.x - vector3.x * 1.1f;
					float f2 = vector5.y - vector3.y * 1.1f;
					float f3 = vector5.z - vector3.z * 1.1f;
					int x2 = Mathf.RoundToInt(f);
					int y2 = Mathf.RoundToInt(f2);
					int z2 = Mathf.RoundToInt(f3);
					voxelNodeType = VoxelTerrain.Get(x2, y2, z2, desiredDepth).NodeType;
				}
				else
				{
					voxelNodeType = fallbackNodeType;
				}
				Vector2 item = Vector2.zero;
				foreach (Vein vein in veins)
				{
					Vector3 vector6 = VoxelTerrain.OctreeToWorldSpace(chunkPosition + array[k]);
					if (vein.VeinBounds.Contains(vector6.RoundToInt()))
					{
						Vector2 crackedEffectColor = vein.GetCrackedEffectColor(vector6);
						if (crackedEffectColor.y > 0f)
						{
							item = crackedEffectColor;
							break;
						}
					}
				}
				minableColor.Add(item);
				byte item2 = (byte)voxelNodeType;
				nodeTypes.Add(item2);
			}
		}
	}

	private static bool ValidateTriangle(Vector3[] triVerts, out Vector3 normal)
	{
		float num = 1E-08f;
		normal = Vector3.zero;
		if ((triVerts[0] - triVerts[1]).sqrMagnitude < num || (triVerts[1] - triVerts[2]).sqrMagnitude < num || (triVerts[2] - triVerts[0]).sqrMagnitude < num)
		{
			return false;
		}
		Vector3 vector = Vector3.Cross(triVerts[1] - triVerts[0], triVerts[2] - triVerts[0]);
		if (vector.sqrMagnitude < num)
		{
			return false;
		}
		normal = vector.normalized;
		return true;
	}

	private static Vector3 ComputeNormal(Vector3 pos, float sampleDist, sbyte desiredDepth)
	{
		int num = 1;
		pos.x = Mathf.Round(pos.x / (float)num) * (float)num;
		pos.y = Mathf.Round(pos.y / (float)num) * (float)num;
		pos.z = Mathf.Round(pos.z / (float)num) * (float)num;
		int num2 = VoxelTerrain.Get(Mathf.RoundToInt(pos.x - sampleDist), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z), desiredDepth).Density - VoxelTerrain.Get(Mathf.RoundToInt(pos.x + sampleDist), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z), desiredDepth).Density;
		int num3 = VoxelTerrain.Get(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y - sampleDist), Mathf.RoundToInt(pos.z), desiredDepth).Density - VoxelTerrain.Get(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y + sampleDist), Mathf.RoundToInt(pos.z), desiredDepth).Density;
		int num4 = VoxelTerrain.Get(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z - sampleDist), desiredDepth).Density - VoxelTerrain.Get(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z + sampleDist), desiredDepth).Density;
		Vector3 vector = new Vector3(num2, num3, num4);
		if (!(vector.sqrMagnitude < 1E-06f))
		{
			return vector.normalized;
		}
		return Vector3.up;
	}

	private float GetOffset(byte v1, byte v2)
	{
		int num = v2 - v1;
		if (num != 0)
		{
			return (float)(Surface - v1) / (float)num;
		}
		return 0.5f;
	}
}
