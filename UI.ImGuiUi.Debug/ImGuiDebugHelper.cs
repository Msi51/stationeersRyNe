using System.Collections.Generic;
using Assets.Scripts.UI.ImGuiUi;
using UnityEngine;

namespace UI.ImGuiUi.Debug;

public static class ImGuiDebugHelper
{
	private static readonly List<DebugDraw> _toDraw = new List<DebugDraw>();

	public static void DrawDebug()
	{
		if (_toDraw.Count <= 0)
		{
			return;
		}
		foreach (DebugDraw item in _toDraw)
		{
			item.Draw();
		}
		_toDraw.Clear();
	}

	private static void Add(DebugDraw debugDraw)
	{
		_toDraw.Add(debugDraw);
	}

	public static void DrawLine(Vector3 start, Vector3 end)
	{
		Add(new Line(start, end, ImGuiColor.Integer.Green));
	}

	public static void DrawLine(Vector3 start, Vector3 end, uint color)
	{
		Add(new Line(start, end, color));
	}

	public static void DrawCube(Vector3 center, Vector3 size)
	{
		Add(new Cube(center, size, ImGuiColor.Integer.Green));
	}

	public static void DrawCube(Vector3 center, Vector3 size, uint color)
	{
		Add(new Cube(center, size, color));
	}

	public static void DrawWireSphere(Vector3 center, float radius)
	{
		Add(new Sphere(center, radius, ImGuiColor.Integer.Green));
	}

	public static void DrawWireSphere(Vector3 center, float radius, uint color)
	{
		Add(new Sphere(center, radius, color));
	}

	public static void DrawText(Vector3 position, string text, int id)
	{
		Add(new Text(position, text, id, ImGuiColor.Integer.Green));
	}

	public static void DrawOverlayText(Vector2 position, string text, int id)
	{
		Add(new OverlayText(position, text, id));
	}
}
