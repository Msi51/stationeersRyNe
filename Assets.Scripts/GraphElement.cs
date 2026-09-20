using Assets.Scripts.UI.ImGuiUi;
using ImGuiNET;
using UnityEngine;

namespace Assets.Scripts;

public class GraphElement
{
	public string Name;

	public bool Visible = true;

	public float[] Points;

	public float MaxValue;

	public float MinValue;

	public Vector4 Color;

	public GraphElement(string name, int length, Vector4 color, bool visible = true)
	{
		Name = name;
		Points = new float[length];
		Color = color;
		Visible = visible;
	}

	public void DrawSetting()
	{
		ImGui.PushStyleColor(ImGuiCol.CheckMark, Color);
		ImGui.Checkbox(Name, ref Visible);
		ImGui.PopStyleColor(1);
	}

	public void Record(float value)
	{
		MaxValue = float.NegativeInfinity;
		MinValue = float.PositiveInfinity;
		for (int i = 1; i < Points.Length; i++)
		{
			Points[i - 1] = Points[i];
			MaxValue = Mathf.Max(MaxValue, Points[i]);
			MinValue = Mathf.Min(MinValue, Points[i]);
		}
		Points[Points.Length - 1] = value;
		MaxValue = Mathf.Max(MaxValue, value);
		MinValue = Mathf.Min(MinValue, value);
	}

	public void Plot(Vector2 cursorPos, Vector2 graphSize, int offset = 0)
	{
		if (Visible)
		{
			ImGui.SetCursorPos(cursorPos);
			ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColor.Float4.Transparent);
			ImGui.PushStyleColor(ImGuiCol.PlotLines, Color);
			ImGui.PlotLines("", ref Points, Points.Length, offset, "", MinValue, MaxValue, graphSize);
			ImGui.PopStyleColor(2);
		}
	}
}
