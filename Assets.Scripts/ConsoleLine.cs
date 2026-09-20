using System;
using System.Text;
using Assets.Scripts.UI.ImGuiUi;
using ImGuiNET;
using UI.ImGuiUi;
using UnityEngine;

namespace Assets.Scripts;

public class ConsoleLine
{
	public uint Color = ConsoleWindow.DefaultColor;

	public string Time = string.Empty;

	public string Text = string.Empty;

	public string[] Continuations;

	public uint[] ContinuationColors;

	public ConsoleSegment[][] SegmentLines;

	private float _activeTime;

	private static readonly uint HoverOverlayColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.1f));

	public void Set(string text, uint color, float activeTime = 5f, uint[] continuationColors = null)
	{
		Continuations = null;
		ContinuationColors = continuationColors;
		SegmentLines = null;
		if (string.IsNullOrEmpty(text))
		{
			Text = string.Empty;
		}
		else if (text.IndexOf('\n') < 0)
		{
			Text = text;
		}
		else
		{
			string[] array = text.Split('\n');
			Text = array[0];
			if (array.Length > 1)
			{
				Continuations = new string[array.Length - 1];
				Array.Copy(array, 1, Continuations, 0, Continuations.Length);
			}
		}
		Color = color;
		Time = DateTime.Now.ToString("HH:mm:ss");
		_activeTime = activeTime;
	}

	public void SetSegments(ConsoleSegment[][] lines, float activeTime = 5f)
	{
		Continuations = null;
		ContinuationColors = null;
		SegmentLines = lines;
		if (lines != null && lines.Length != 0 && lines[0].Length != 0)
		{
			Color = lines[0][0].Color;
			Text = lines[0][0].Text;
		}
		else
		{
			Color = ConsoleWindow.DefaultColor;
			Text = string.Empty;
		}
		Time = DateTime.Now.ToString("HH:mm:ss");
		_activeTime = activeTime;
	}

	public void Draw(ref Vector2 inputSize, bool isShown, bool noFade = false)
	{
		if (string.IsNullOrEmpty(Text) || (!isShown && _activeTime <= 0f))
		{
			return;
		}
		bool flag = false;
		if (!isShown & (_activeTime <= 1f))
		{
			float val = ((!noFade) ? Mathf.Lerp(0f, 1f, _activeTime) : ((_activeTime > 0f) ? 1f : 0f));
			ImGui.PushStyleVar(ImGuiStyleVar.Alpha, val);
			flag = true;
		}
		float cursorPosY = ImGui.GetCursorPosY();
		ImGui.BeginGroup();
		ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColor.Integer.Grey);
		ImGui.Text(Time);
		ImGui.PopStyleColor();
		ImGui.SameLine();
		GetLevelGlyph(Color, out var glyph, out var glyphColor);
		ImGui.PushStyleColor(ImGuiCol.Text, glyphColor);
		ImGui.Text(glyph);
		ImGui.PopStyleColor();
		ImGui.SameLine();
		float cursorPosX = ImGui.GetCursorPosX();
		if (SegmentLines != null)
		{
			for (int i = 0; i < SegmentLines.Length; i++)
			{
				if (i > 0)
				{
					ImGui.SetCursorPosX(cursorPosX);
				}
				DrawSegmentRow(SegmentLines[i], isShown);
			}
		}
		else
		{
			DrawBody(Text, Color, isShown);
			if (Continuations != null)
			{
				for (int j = 0; j < Continuations.Length; j++)
				{
					string text = Continuations[j];
					if (!string.IsNullOrEmpty(text))
					{
						ImGui.SetCursorPosX(cursorPosX);
						uint color = ((ContinuationColors != null && j < ContinuationColors.Length) ? ContinuationColors[j] : Color);
						DrawBody(text, color, isShown);
					}
				}
			}
		}
		ImGui.EndGroup();
		if (isShown && ImGui.IsItemHovered())
		{
			ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), HoverOverlayColor);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
			{
				CopyToClipboard();
			}
		}
		inputSize += new Vector2(0f, ImGui.GetCursorPosY() - cursorPosY) + ImGui.GetStyle().ItemSpacing;
		if (flag)
		{
			ImGui.PopStyleVar();
		}
		if (_activeTime > 0f)
		{
			_activeTime -= UnityEngine.Time.unscaledDeltaTime;
		}
	}

	private static void DrawBody(string text, uint color, bool isShown)
	{
		int num = text.IndexOf('\t');
		if (isShown && num >= 0)
		{
			DrawTextWrapped(text.Substring(0, num), color);
			ImGui.SameLine(0f, 0f);
			DrawTextWrapped(text.Substring(num + 1), color);
		}
		else
		{
			DrawTextWrapped(text, color, isShown);
		}
	}

	private static void DrawSegmentRow(ConsoleSegment[] segments, bool isShown)
	{
		if (segments == null || segments.Length == 0)
		{
			return;
		}
		int num = -1;
		for (int num2 = segments.Length - 1; num2 >= 0; num2--)
		{
			if (!string.IsNullOrEmpty(segments[num2].Text))
			{
				num = num2;
				break;
			}
		}
		if (num < 0)
		{
			return;
		}
		for (int i = 0; i < segments.Length; i++)
		{
			ConsoleSegment consoleSegment = segments[i];
			if (string.IsNullOrEmpty(consoleSegment.Text))
			{
				continue;
			}
			if (i == num)
			{
				DrawTextWrapped(consoleSegment.Text, consoleSegment.Color, isShown);
				continue;
			}
			if (isShown)
			{
				ImGui.PushStyleColor(ImGuiCol.Text, consoleSegment.Color);
				ImGui.TextUnformatted(consoleSegment.Text);
				ImGui.PopStyleColor();
			}
			else
			{
				ImguiHelper.TextShadow(consoleSegment.Text, consoleSegment.Color, TextAlignment.Left, unformatted: true);
			}
			ImGui.SameLine(0f, 0f);
		}
	}

	private static void DrawTextWrapped(string text, uint color, bool isShown = true)
	{
		ImGui.PushTextWrapPos(0f);
		if (!isShown)
		{
			ImguiHelper.TextShadow(text, color, TextAlignment.Left, unformatted: true);
		}
		else
		{
			ImGui.PushStyleColor(ImGuiCol.Text, color);
			ImGui.TextUnformatted(text);
			ImGui.PopStyleColor();
		}
		ImGui.PopTextWrapPos();
	}

	private static void GetLevelGlyph(uint lineColor, out string glyph, out uint glyphColor)
	{
		if (lineColor == ImGuiColor.Integer.Red || lineColor == ImGuiColor.Integer.Magenta)
		{
			glyph = "*";
			glyphColor = ImGuiColor.Integer.Red;
		}
		else if (lineColor == ImGuiColor.Integer.Yellow)
		{
			glyph = "!";
			glyphColor = ImGuiColor.Integer.Yellow;
		}
		else if (lineColor == ImGuiColor.Integer.LightBlue)
		{
			glyph = ">";
			glyphColor = ImGuiColor.Integer.LightBlue;
		}
		else
		{
			glyph = "-";
			glyphColor = ImGuiColor.Integer.DarkGrey;
		}
	}

	private void CopyToClipboard()
	{
		GUIUtility.systemCopyBuffer = ((SegmentLines != null) ? FlattenSegments() : FlattenLegacy());
		ConsoleWindow.FlashCopyToast();
	}

	private string FlattenLegacy()
	{
		if (Continuations != null)
		{
			return Text + "\n" + string.Join("\n", Continuations);
		}
		return Text;
	}

	private string FlattenSegments()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < SegmentLines.Length; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append('\n');
			}
			ConsoleSegment[] array = SegmentLines[i];
			for (int j = 0; j < array.Length; j++)
			{
				ConsoleSegment consoleSegment = array[j];
				if (!string.IsNullOrEmpty(consoleSegment.Text))
				{
					stringBuilder.Append(consoleSegment.Text);
				}
			}
		}
		return stringBuilder.ToString();
	}

	public void Clear()
	{
		Text = string.Empty;
		Time = string.Empty;
		Continuations = null;
		ContinuationColors = null;
		SegmentLines = null;
		Color = ConsoleWindow.DefaultColor;
	}

	public void Apply(ConsoleLine consoleLine)
	{
		Text = consoleLine.Text;
		Time = consoleLine.Time;
		Color = consoleLine.Color;
		Continuations = consoleLine.Continuations;
		ContinuationColors = consoleLine.ContinuationColors;
		SegmentLines = consoleLine.SegmentLines;
		_activeTime = consoleLine._activeTime;
	}

	public override string ToString()
	{
		if (Continuations == null)
		{
			return Time + ": " + Text;
		}
		return Time + ": " + Text + "\n" + string.Join("\n", Continuations);
	}
}
