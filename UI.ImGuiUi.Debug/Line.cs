using Assets.Scripts.UI.ImGuiUi;
using UnityEngine;

namespace UI.ImGuiUi.Debug;

public class Line : DebugDraw
{
	private Vector3 _start;

	private Vector3 _end;

	public Line(Vector3 start, Vector3 end, uint color)
	{
		_start = start;
		_end = end;
		_color = color;
	}

	public override void Draw()
	{
		ImGuiExtensions.Rendering.RenderingColor = _color;
		ImGuiExtensions.Rendering.DrawLineSimple(_start, _end);
	}
}
