using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Util.Splines;

namespace Objects.Electrical;

public static class CableMeshBuilder
{
	public static Mesh Build(ISpline spline, Vector2[] crossSection, float ringSpacing, Transform worldToLocal)
	{
		float num = Mathf.Max(spline.EstimatedActualLength, 0.001f);
		int num2 = Mathf.Max(2, Mathf.CeilToInt(num / ringSpacing) + 1);
		int num3 = crossSection.Length;
		(float[] cum, float[] ts) tuple = BuildArcTable(spline, 64);
		float[] item = tuple.cum;
		float[] item2 = tuple.ts;
		float num4 = item[^1];
		List<Vector3> list = new List<Vector3>((num2 + 2) * num3);
		List<Vector2> list2 = new List<Vector2>((num2 + 2) * num3);
		List<int> list3 = new List<int>((num2 - 1) * num3 * 6 + (num3 - 2) * 6);
		for (int i = 0; i < num2; i++)
		{
			float num5 = (float)i / (float)(num2 - 1) * num4;
			float t = TForDistance(item, item2, num5);
			Vector3 position = spline.GetPosition(t);
			Quaternion rotation = spline.GetRotation(t);
			for (int j = 0; j < num3; j++)
			{
				Vector3 position2 = position + rotation * new Vector3(crossSection[j].x, crossSection[j].y, 0f);
				list.Add(worldToLocal.InverseTransformPoint(position2));
				list2.Add(new Vector2((float)j / (float)(num3 - 1), num5));
			}
		}
		for (int k = 0; k < num2 - 1; k++)
		{
			int num6 = k * num3;
			int num7 = (k + 1) * num3;
			for (int l = 0; l < num3; l++)
			{
				int num8 = (l + 1) % num3;
				list3.Add(num6 + l);
				list3.Add(num6 + num8);
				list3.Add(num7 + l);
				list3.Add(num6 + num8);
				list3.Add(num7 + num8);
				list3.Add(num7 + l);
			}
		}
		int count = list.Count;
		for (int m = 0; m < num3; m++)
		{
			list.Add(list[m]);
			list2.Add(new Vector2(0.5f + crossSection[m].x, 0.5f + crossSection[m].y));
		}
		for (int n = 1; n < num3 - 1; n++)
		{
			list3.Add(count);
			list3.Add(count + n + 1);
			list3.Add(count + n);
		}
		int num9 = (num2 - 1) * num3;
		int count2 = list.Count;
		for (int num10 = 0; num10 < num3; num10++)
		{
			list.Add(list[num9 + num10]);
			list2.Add(new Vector2(0.5f + crossSection[num10].x, 0.5f + crossSection[num10].y));
		}
		for (int num11 = 1; num11 < num3 - 1; num11++)
		{
			list3.Add(count2);
			list3.Add(count2 + num11);
			list3.Add(count2 + num11 + 1);
		}
		Mesh mesh = new Mesh
		{
			name = "PylonCableMesh"
		};
		if (list.Count > 65535)
		{
			mesh.indexFormat = IndexFormat.UInt32;
		}
		mesh.SetVertices(list);
		mesh.SetUVs(0, list2);
		mesh.SetTriangles(list3, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	private static (float[] cum, float[] ts) BuildArcTable(ISpline spline, int samples)
	{
		float[] array = new float[samples + 1];
		float[] array2 = new float[samples + 1];
		Vector3 a = spline.GetPosition(0f);
		for (int i = 1; i <= samples; i++)
		{
			float num = (float)i / (float)samples;
			Vector3 position = spline.GetPosition(num);
			array[i] = array[i - 1] + Vector3.Distance(a, position);
			array2[i] = num;
			a = position;
		}
		return (cum: array, ts: array2);
	}

	private static float TForDistance(float[] cum, float[] ts, float s)
	{
		int num = cum.Length - 1;
		if (s <= 0f)
		{
			return 0f;
		}
		if (s >= cum[num])
		{
			return 1f;
		}
		for (int i = 1; i <= num; i++)
		{
			if (cum[i] >= s)
			{
				return Mathf.Lerp(ts[i - 1], ts[i], Mathf.InverseLerp(cum[i - 1], cum[i], s));
			}
		}
		return 1f;
	}
}
