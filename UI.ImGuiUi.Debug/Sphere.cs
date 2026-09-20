using Assets.Scripts.UI.ImGuiUi;
using UnityEngine;

namespace UI.ImGuiUi.Debug;

public class Sphere : DebugDraw
{
	private Vector3 _center;

	private float _radius;

	public Sphere(Vector3 center, float radius, uint color)
	{
		_center = center;
		_radius = radius;
		_color = color;
	}

	public override void Draw()
	{
		ImGuiExtensions.Rendering.RenderingColor = _color;
		ImGuiExtensions.Rendering.DrawWireSphere(_center, _radius);
	}
}
