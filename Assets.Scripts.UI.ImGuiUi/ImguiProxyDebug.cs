using System;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public static class ImguiProxyDebug
{
	private static readonly Vector3[] Corners = new Vector3[8]
	{
		new Vector3(-0.5f, -0.5f, -0.5f),
		new Vector3(0.5f, -0.5f, -0.5f),
		new Vector3(0.5f, -0.5f, 0.5f),
		new Vector3(-0.5f, -0.5f, 0.5f),
		new Vector3(-0.5f, 0.5f, -0.5f),
		new Vector3(0.5f, 0.5f, -0.5f),
		new Vector3(0.5f, 0.5f, 0.5f),
		new Vector3(-0.5f, 0.5f, 0.5f)
	};

	private static readonly int[] Edges = new int[24]
	{
		0, 1, 1, 2, 2, 3, 3, 0, 4, 5,
		5, 6, 6, 7, 7, 4, 0, 4, 1, 5,
		2, 6, 3, 7
	};

	public static void DrawDebug()
	{
		OcclusionManager instance = OcclusionManager.Instance;
		if (instance == null || !instance.DistantProxyDebugDraw || !instance.TryGetPublishedProxies(out var matrices, out var count) || count <= 0 || Camera.main == null)
		{
			return;
		}
		TerrainCurvature.Refresh();
		Vector3 position = Camera.main.transform.position;
		ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.Green;
		Span<Vector3> span = stackalloc Vector3[8];
		for (int i = 0; i < count; i++)
		{
			Matrix4x4 matrix4x = matrices[i];
			for (int j = 0; j < 8; j++)
			{
				Vector3 vector = matrix4x.MultiplyPoint3x4(Corners[j]);
				if (vector.y < 1000f)
				{
					vector = TerrainCurvature.Curve(vector, position);
				}
				span[j] = ImGuiExtensions.WorldToScreen(vector);
			}
			for (int k = 0; k < Edges.Length; k += 2)
			{
				ImGuiExtensions.Rendering.DrawClippedLine(span[Edges[k]], span[Edges[k + 1]]);
			}
		}
	}
}
