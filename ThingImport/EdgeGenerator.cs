using System.Collections.Generic;
using System.Diagnostics;
using Assets.Scripts;
using Assets.Scripts.Util;
using UnityEngine;

namespace ThingImport;

public static class EdgeGenerator
{
	public static List<Edge> GenerateSharpEdges(Mesh mesh, float angleThreshold, float mergeDistance)
	{
		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		for (int i = 0; i < vertices.Length; i++)
		{
			Vector3 a = vertices[i];
			for (int j = 0; j < vertices.Length; j++)
			{
				if (!dictionary.ContainsKey(j))
				{
					Vector3 b = vertices[j];
					if (RocketMath.Approximately(a, b, mergeDistance))
					{
						dictionary.Add(j, i);
					}
				}
			}
		}
		ConsoleWindow.Print($"Weld vertices mapping created in {stopwatch.ElapsedMilliseconds}ms");
		stopwatch.Restart();
		Dictionary<(int, int), List<int>> dictionary2 = new Dictionary<(int, int), List<int>>();
		Vector3[] array = new Vector3[triangles.Length / 3];
		for (int k = 0; k < triangles.Length; k += 3)
		{
			int num = k / 3;
			int num2 = dictionary[triangles[k]];
			int num3 = dictionary[triangles[k + 1]];
			int num4 = dictionary[triangles[k + 2]];
			Vector3 vector = vertices[num2];
			Vector3 vector2 = vertices[num3];
			Vector3 vector3 = vertices[num4];
			CheckTrianglesTouchedByEdge(num2, num3, num, dictionary2);
			CheckTrianglesTouchedByEdge(num3, num4, num, dictionary2);
			CheckTrianglesTouchedByEdge(num4, num2, num, dictionary2);
			Vector3 normalized = Vector3.Cross(vector2 - vector, vector3 - vector).normalized;
			array[num] = normalized;
		}
		ConsoleWindow.Print($"Edge to triangle mapping created in {stopwatch.ElapsedMilliseconds}ms");
		stopwatch.Restart();
		List<Edge> list = new List<Edge>();
		foreach (KeyValuePair<(int, int), List<int>> item in dictionary2)
		{
			Vector3 a2 = vertices[item.Key.Item1];
			Vector3 b2 = vertices[item.Key.Item2];
			if (item.Value.Count == 2)
			{
				Vector3 vector4 = array[item.Value[0]];
				Vector3 to = array[item.Value[1]];
				if (Vector3.Angle(vector4, to) > angleThreshold)
				{
					list.Add(new Edge(a2, b2));
				}
			}
			else if (item.Value.Count == 1)
			{
				list.Add(new Edge(a2, b2));
			}
		}
		ConsoleWindow.Print($"Edges list created in {stopwatch.ElapsedMilliseconds}ms");
		stopwatch.Stop();
		return list;
	}

	private static void CheckTrianglesTouchedByEdge(int e0, int e1, int ti, Dictionary<(int, int), List<int>> lookup)
	{
		(int, int) key = ((e0 > e1) ? (e0, e1) : (e1, e0));
		if (!lookup.ContainsKey(key))
		{
			lookup.Add(key, new List<int>());
		}
		lookup[key].Add(ti);
	}
}
