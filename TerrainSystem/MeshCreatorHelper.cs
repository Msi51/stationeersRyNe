using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace TerrainSystem;

public static class MeshCreatorHelper
{
	public static Mesh CreateLavaMesh(LavaData lavaData)
	{
		float num = (float)VoxelConstants.Size / 256f;
		List<Vector3> list = new List<Vector3>(65536);
		List<Vector2> list2 = new List<Vector2>(65536);
		List<int> list3 = new List<int>(393216);
		for (int i = 0; i < 256; i++)
		{
			for (int j = 0; j < 256; j++)
			{
				float x = (float)j / 256f;
				float y = (float)i / 256f;
				int lavaHeightForMeshGeneration = lavaData.GetLavaHeightForMeshGeneration(j, i);
				list.Add(new Vector3((float)j * num, lavaHeightForMeshGeneration, (float)i * num));
				list2.Add(new Vector2(x, y));
			}
		}
		for (int k = 0; k < 255; k++)
		{
			for (int l = 0; l < 255; l++)
			{
				int num2 = l + k * 256;
				list3.Add(num2);
				list3.Add(num2 + 256);
				list3.Add(num2 + 256 + 1);
				list3.Add(num2);
				list3.Add(num2 + 256 + 1);
				list3.Add(num2 + 1);
			}
		}
		Mesh mesh = new Mesh();
		mesh.vertices = list.ToArray();
		mesh.uv = list2.ToArray();
		mesh.triangles = list3.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	public static Mesh CreateMeshServer(List<Vector3> verts, List<int> indices)
	{
		if (verts.Count == 0 || indices.Count == 0)
		{
			return null;
		}
		Mesh mesh = new Mesh();
		Mesh.MeshDataArray data = Mesh.AllocateWritableMeshData(1);
		Mesh.MeshData meshData = data[0];
		meshData.SetVertexBufferParams(verts.Count, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0), new VertexAttributeDescriptor(VertexAttribute.Normal), new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4), new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4), new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2), new VertexAttributeDescriptor(VertexAttribute.TexCoord1));
		NativeArray<float> vertexData = meshData.GetVertexData<float>();
		for (int i = 0; i < verts.Count; i++)
		{
			int num = i * 19;
			vertexData[num] = verts[i].x;
			vertexData[num + 1] = verts[i].y;
			vertexData[num + 2] = verts[i].z;
			vertexData[num + 3] = 1f;
			vertexData[num + 4] = 0f;
			vertexData[num + 5] = 0f;
			vertexData[num + 6] = 1f;
			vertexData[num + 7] = 0f;
			vertexData[num + 8] = 0f;
			vertexData[num + 9] = 1f;
			vertexData[num + 10] = 0f;
			vertexData[num + 11] = 0f;
			vertexData[num + 12] = 0f;
			vertexData[num + 13] = 0f;
			vertexData[num + 14] = 0f;
			vertexData[num + 15] = 0f;
			vertexData[num + 16] = 1f;
			vertexData[num + 17] = 0f;
			vertexData[num + 18] = 0f;
		}
		meshData.SetIndexBufferParams(indices.Count, IndexFormat.UInt32);
		NativeArray<int> indexData = meshData.GetIndexData<int>();
		NativeArray<int>.Copy(indices.ToArray(), indexData);
		meshData.subMeshCount = 1;
		meshData.SetSubMesh(0, new SubMeshDescriptor(0, indices.Count));
		mesh.Clear();
		Mesh.ApplyAndDisposeWritableMeshData(data, mesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
		mesh.RecalculateBounds();
		Vector2[] array = new Vector2[verts.Count];
		for (int j = 0; j < verts.Count; j++)
		{
			Vector3 vector = verts[j];
			array[j] = new Vector2(vector.x, vector.z);
		}
		mesh.uv = array;
		mesh.RecalculateTangents();
		return mesh;
	}

	public static Mesh CreateMesh(List<Vector3> verts, List<Vector3> normals, List<Vector3> deformedNormals, List<byte> voxelType, List<Vector2> minableColorHueValue, List<int> indices)
	{
		if (verts.Count == 0 || verts.Count != normals.Count || voxelType.Count != verts.Count || indices.Count == 0)
		{
			return null;
		}
		Mesh mesh = new Mesh();
		Mesh.MeshDataArray data = Mesh.AllocateWritableMeshData(1);
		Mesh.MeshData meshData = data[0];
		meshData.SetVertexBufferParams(verts.Count, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0), new VertexAttributeDescriptor(VertexAttribute.Normal), new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4), new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4), new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2), new VertexAttributeDescriptor(VertexAttribute.TexCoord1));
		NativeArray<float> vertexData = meshData.GetVertexData<float>();
		for (int i = 0; i < verts.Count; i++)
		{
			int num = i * 19;
			vertexData[num] = verts[i].x;
			vertexData[num + 1] = verts[i].y;
			vertexData[num + 2] = verts[i].z;
			vertexData[num + 3] = normals[i].x;
			vertexData[num + 4] = normals[i].y;
			vertexData[num + 5] = normals[i].z;
			vertexData[num + 6] = 1f;
			vertexData[num + 7] = 0f;
			vertexData[num + 8] = 0f;
			vertexData[num + 9] = 1f;
			bool flag = (voxelType[i] & 1) != 0;
			bool flag2 = (voxelType[i] & 2) != 0;
			bool flag3 = (voxelType[i] & 4) != 0;
			bool num2 = (voxelType[i] & 8) != 0;
			float value = 0f;
			if (flag && flag2)
			{
				value = 0.5f;
			}
			else if (flag)
			{
				value = 0f;
			}
			else if (flag2)
			{
				value = 1f;
			}
			float value2 = 0.5f;
			if (num2)
			{
				value2 = 0f;
			}
			else if (flag3)
			{
				value2 = 1f;
			}
			vertexData[num + 10] = minableColorHueValue[i].x;
			vertexData[num + 11] = minableColorHueValue[i].y;
			vertexData[num + 12] = value;
			vertexData[num + 13] = value2;
			vertexData[num + 14] = 0f;
			vertexData[num + 15] = 0f;
			vertexData[num + 16] = deformedNormals[i].x;
			vertexData[num + 17] = deformedNormals[i].y;
			vertexData[num + 18] = deformedNormals[i].z;
		}
		meshData.SetIndexBufferParams(indices.Count, IndexFormat.UInt32);
		NativeArray<int> indexData = meshData.GetIndexData<int>();
		NativeArray<int>.Copy(indices.ToArray(), indexData);
		meshData.subMeshCount = 1;
		meshData.SetSubMesh(0, new SubMeshDescriptor(0, indices.Count));
		mesh.Clear();
		Mesh.ApplyAndDisposeWritableMeshData(data, mesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
		mesh.RecalculateBounds();
		Vector2[] array = new Vector2[verts.Count];
		for (int j = 0; j < verts.Count; j++)
		{
			Vector3 vector = verts[j];
			array[j] = new Vector2(vector.x, vector.z);
		}
		mesh.uv = array;
		mesh.RecalculateTangents();
		return mesh;
	}
}
