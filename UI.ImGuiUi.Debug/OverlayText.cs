using ImGuiNET;
using UnityEngine;

namespace UI.ImGuiUi.Debug;

public class OverlayText : DebugDraw
{
	private int _id;

	private Vector2 _position;

	private string _text;

	public OverlayText(Vector2 position, string text, int id)
	{
		_id = id;
		_position = position;
		_text = text;
	}

	public override void Draw()
	{
		ImGui.Begin($"debug{_id}", (ImGuiWindowFlags)799685);
		ImGui.SetWindowPos(_position, ImGuiCond.Always);
		ImguiHelper.DrawText(_text);
		ImGui.End();
	}
}
