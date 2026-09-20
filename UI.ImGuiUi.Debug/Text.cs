using Assets.Scripts.UI.ImGuiUi;
using UnityEngine;

namespace UI.ImGuiUi.Debug;

public class Text : DebugDraw
{
	private Vector3 _center;

	private string _text;

	private int _id;

	public Text(Vector3 center, string text, int id, uint color)
	{
		_center = center;
		_text = text;
		_id = id;
		_color = color;
	}

	public override void Draw()
	{
		ImGuiExtensions.Rendering.RenderingColor = _color;
		ImGuiExtensions.Rendering.DrawTextInWorld(_text, _center, _id);
	}
}
