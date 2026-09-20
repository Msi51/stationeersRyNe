using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace ThingImport;

public class OBJImporter
{
	public class MeshData
	{
		public Vector3[] vertices;

		public Vector2[] uvs;

		public Vector3[] normals;

		public int[] triangles;
	}

	public static MeshData ImportOBJ(string objText, float scale)
	{
		List<Vector3> list = new List<Vector3>();
		List<Vector2> list2 = new List<Vector2>();
		List<Vector3> list3 = new List<Vector3>();
		List<int> list4 = new List<int>();
		List<Vector3> list5 = new List<Vector3>();
		List<Vector2> list6 = new List<Vector2>();
		List<Vector3> list7 = new List<Vector3>();
		Dictionary<(int, int, int), int> vertexCache = new Dictionary<(int, int, int), int>();
		using (StringReader stringReader = new StringReader(objText))
		{
			string text;
			while ((text = stringReader.ReadLine()) != null)
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				string[] array = text.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length != 0)
				{
					switch (array[0])
					{
					case "v":
						list5.Add(new Vector3(float.Parse(array[1], CultureInfo.InvariantCulture) * scale, float.Parse(array[2], CultureInfo.InvariantCulture) * scale, float.Parse(array[3], CultureInfo.InvariantCulture) * scale));
						break;
					case "vt":
						list6.Add(new Vector2(float.Parse(array[1], CultureInfo.InvariantCulture), float.Parse(array[2], CultureInfo.InvariantCulture)));
						break;
					case "vn":
						list7.Add(new Vector3(float.Parse(array[1], CultureInfo.InvariantCulture), float.Parse(array[2], CultureInfo.InvariantCulture), float.Parse(array[3], CultureInfo.InvariantCulture)));
						break;
					case "f":
						ProcessFace(array, list5, list6, list7, list, list2, list3, list4, vertexCache);
						break;
					}
				}
			}
		}
		return ConvertRightToLeftHanded(new MeshData
		{
			vertices = list.ToArray(),
			uvs = list2.ToArray(),
			normals = list3.ToArray(),
			triangles = list4.ToArray()
		});
	}

	private static void PushFace(string[] parts, List<Vector3> tempVertices, List<Vector2> tempUVs, List<Vector3> tempNormals, List<Vector3> vertices, List<Vector2> uvs, List<Vector3> normals, List<int> triangles, Dictionary<(int, int, int), int> vertexCache)
	{
		for (int i = 0; i < 3; i++)
		{
			string[] array = parts[i].Split('/');
			int num = int.Parse(array[0], CultureInfo.InvariantCulture) - 1;
			int num2 = ((array.Length > 1 && !string.IsNullOrEmpty(array[1])) ? (int.Parse(array[1], CultureInfo.InvariantCulture) - 1) : (-1));
			int num3 = ((array.Length > 2) ? (int.Parse(array[2], CultureInfo.InvariantCulture) - 1) : (-1));
			(int, int, int) key = (num, num2, num3);
			if (!vertexCache.TryGetValue(key, out var value))
			{
				vertices.Add(tempVertices[num]);
				if (num2 >= 0 && num2 < tempUVs.Count)
				{
					uvs.Add(tempUVs[num2]);
				}
				else
				{
					uvs.Add(Vector2.zero);
				}
				if (num3 >= 0 && num3 < tempNormals.Count)
				{
					normals.Add(tempNormals[num3]);
				}
				else
				{
					normals.Add(Vector3.up);
				}
				value = vertices.Count - 1;
				vertexCache.Add(key, value);
			}
			triangles.Add(value);
		}
	}

	private static void ProcessFace(string[] parts, List<Vector3> tempVertices, List<Vector2> tempUVs, List<Vector3> tempNormals, List<Vector3> vertices, List<Vector2> uvs, List<Vector3> normals, List<int> triangles, Dictionary<(int, int, int), int> vertexCache)
	{
		if (parts.Length == 4)
		{
			PushFace(new string[3]
			{
				parts[1],
				parts[2],
				parts[3]
			}, tempVertices, tempUVs, tempNormals, vertices, uvs, normals, triangles, vertexCache);
		}
		else if (parts.Length == 5)
		{
			PushFace(new string[3]
			{
				parts[1],
				parts[2],
				parts[3]
			}, tempVertices, tempUVs, tempNormals, vertices, uvs, normals, triangles, vertexCache);
			PushFace(new string[3]
			{
				parts[3],
				parts[4],
				parts[1]
			}, tempVertices, tempUVs, tempNormals, vertices, uvs, normals, triangles, vertexCache);
		}
		else
		{
			_ = parts.Length;
			_ = 4;
		}
	}

	private static MeshData ConvertRightToLeftHanded(MeshData meshData)
	{
		Vector3[] array = new Vector3[meshData.vertices.Length];
		Vector3[] array2 = new Vector3[meshData.normals.Length];
		int[] array3 = new int[meshData.triangles.Length];
		for (int i = 0; i < meshData.vertices.Length; i++)
		{
			array[i] = new Vector3(0f - meshData.vertices[i].x, meshData.vertices[i].y, meshData.vertices[i].z);
		}
		for (int j = 0; j < meshData.normals.Length; j++)
		{
			array2[j] = new Vector3(0f - meshData.normals[j].x, meshData.normals[j].y, meshData.normals[j].z);
		}
		for (int k = 0; k < meshData.triangles.Length; k += 3)
		{
			array3[k] = meshData.triangles[k];
			array3[k + 1] = meshData.triangles[k + 2];
			array3[k + 2] = meshData.triangles[k + 1];
		}
		return new MeshData
		{
			vertices = array,
			uvs = meshData.uvs,
			normals = array2,
			triangles = array3
		};
	}
}
