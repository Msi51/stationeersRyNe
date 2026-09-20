using Assets.Scripts.UI.ImGuiUi;
using UnityEngine;

namespace UI.ImGuiUi.Debug;

public class Cube : DebugDraw
{
	private Vector3 _center;

	private Vector3 _size;

	public Cube(Vector3 center, Vector3 size, uint color)
	{
		_center = center;
		_size = size;
		_color = color;
	}

	public override void Draw()
	{
		ImGuiExtensions.Rendering.RenderingColor = _color;
		ImGuiExtensions.Rendering.DrawCube(_center, _size);
	}
}
