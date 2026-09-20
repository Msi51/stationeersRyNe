using ImGuiNET;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public class GraphWithKeys
{
	public GraphElement GraphElement;

	public AnimationCurveData AnimationCurveData;

	public string Id;

	public int StartingXValue;

	public GraphWithKeys()
	{
	}

	public void DrawGraph(Vector2 cursorPos, Vector2 graphSize)
	{
		GraphElement?.Plot(cursorPos, graphSize);
	}

	public void DrawKeyFrames()
	{
		ImGui.Text(Id + "_Keys");
		AnimationCurveData?.Draw();
		if (AnimationCurveData != null && AnimationCurveData.Apply())
		{
			RefreshRecord();
		}
	}

	public void DrawGraphToggle()
	{
		GraphElement?.DrawSetting();
	}

	public void RefreshRecord()
	{
		GraphElement = new GraphElement(GraphElement.Name, GraphElement.Points.Length, GraphElement.Color);
		for (int i = StartingXValue; i < GraphElement.Points.Length + StartingXValue; i++)
		{
			GraphElement.Record(AnimationCurveData.Curve.Evaluate(i));
		}
	}

	public GraphWithKeys(AnimationCurveData animationCurveData, string name, int dataPoints, Vector4 color, int startingXValue = 0)
	{
		Id = name;
		GraphElement = new GraphElement(name, dataPoints, color);
		AnimationCurveData = animationCurveData;
		StartingXValue = startingXValue;
		for (int i = startingXValue; i < dataPoints; i++)
		{
			GraphElement.Record(AnimationCurveData.Curve.Evaluate(i));
		}
	}
}
