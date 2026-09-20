using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using ImGuiNET;
using UnityEngine;

namespace UI.ImGuiUi;

public static class ImguiHelper
{
	public static float UIScale = 1f;

	public static float PaddingScaled = 5f * UIScale;

	public static Vector2 StandardResizableScaled = new Vector2(600f, 400f) * UIScale;

	public static Vector2 ScreenSize => new Vector2(Screen.width, Screen.height);

	public static Vector2 ScreenCenter => new Vector2((float)Screen.width / 2f, (float)Screen.height / 2f);

	public static bool Draw<T>(string variable, ref int value, List<T> valueOptions) where T : IComboable
	{
		bool result = false;
		ImGui.Text(variable);
		ImGui.NextColumn();
		ImGui.PushItemWidth(-1f);
		T val = ((valueOptions != null && value < valueOptions.Count) ? valueOptions[value] : default(T));
		if (ImGui.BeginCombo("###" + variable, val?.GetName() ?? GameStrings.None.DisplayString, ImGuiComboFlags.HeightRegular))
		{
			if (valueOptions != null)
			{
				for (int i = 0; i < valueOptions.Count; i++)
				{
					if (ImGui.Selectable(valueOptions[i].GetName(), value == i))
					{
						value = i;
						result = true;
					}
				}
			}
			ImGui.EndCombo();
		}
		ImGui.PopItemWidth();
		ImGui.NextColumn();
		return result;
	}

	public static bool DrawCombo<T>(Assets.Scripts.Localization2.GameString variable, ref int value, List<T> valueOptions) where T : IComboable
	{
		return Draw(variable.DisplayString, ref value, valueOptions);
	}

	public static bool DrawCombo<T1, T2>(string variable, ref T1 value, EnumCollection<T1, T2> collection, Assets.Scripts.Localization2.GameString tooltip = null) where T1 : Enum, IConvertible, new() where T2 : IConvertible, IEquatable<T2>
	{
		return DrawCombo(variable, ref value, collection, tooltip?.DisplayString);
	}

	private static bool DrawCombo<T1, T2>(string variable, ref T1 value, EnumCollection<T1, T2> collection, string tooltip = null) where T1 : Enum, IConvertible, new() where T2 : IConvertible, IEquatable<T2>
	{
		ImGui.Text(variable);
		ImGui.NextColumn();
		ImGui.PushItemWidth(-1f);
		bool result = MakeComboBox(variable, ref value, collection, collection.Values, tooltip);
		ImGui.PopItemWidth();
		ImGui.NextColumn();
		return result;
	}

	private static bool MakeComboBox<T1, T2>(string variable, ref T1 value, EnumCollection<T1, T2> collection, T1[] valueOptions, string tooltip = null) where T1 : Enum, IConvertible, new() where T2 : IConvertible, IEquatable<T2>
	{
		bool result = false;
		string[] names = collection.Names;
		int num = collection.GetIndexFromValue(value);
		if (ImGui.BeginCombo("###" + variable, names[num], ImGuiComboFlags.HeightRegular))
		{
			for (int i = 0; i < valueOptions.Length; i++)
			{
				if (ImGui.Selectable(names[i], num == i))
				{
					num = i;
					value = valueOptions[i];
					result = true;
				}
			}
			ImGui.EndCombo();
		}
		if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
		{
			ShowTooltip(tooltip);
		}
		return result;
	}

	public static bool DrawInput<T>(Assets.Scripts.Localization2.GameString gameString, ref T value, Assets.Scripts.Localization2.GameString toolTip = null, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
	{
		return DrawInput(gameString.DisplayString, ref value, toolTip?.DisplayString, flags);
	}

	public static bool DrawInput<T>(string displayName, ref T value, string toolTip = "", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
	{
		if (typeof(T) == typeof(string))
		{
			return DrawTextInput(displayName, ref __refvalue(__makeref(value), string), 256u, isError: false, canBeNull: true, flags, toolTip);
		}
		throw new NotImplementedException("type T for " + displayName + " is not implemented");
	}

	public static bool DrawTextInput(string variable, ref string value, uint maxSize, bool isError, bool canBeNull, ImGuiInputTextFlags flags, string tooltip)
	{
		ImGui.Text(variable);
		ImGui.NextColumn();
		ImGui.PushItemWidth(-1f);
		bool flag = isError || (!canBeNull && string.IsNullOrEmpty(value));
		if (flag)
		{
			ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColor.Float4.DarkRed);
			ImGui.PushStyleColor(ImGuiCol.Border, ImGuiColor.Float4.Red);
		}
		if (string.IsNullOrEmpty(value))
		{
			value = string.Empty;
		}
		bool result = DrawInputText(variable, ref value, maxSize, flags);
		if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
		{
			ShowTooltip(tooltip);
		}
		ImGui.PopItemWidth();
		if (flag)
		{
			ImGui.PopStyleColor(2);
		}
		ImGui.NextColumn();
		return result;
	}

	private static bool DrawInputText(string variable, ref string value, uint maxSize, ImGuiInputTextFlags flags)
	{
		if (string.IsNullOrEmpty(value))
		{
			value = string.Empty;
		}
		if (!ImGui.InputText("###" + variable, ref value, maxSize, flags))
		{
			return false;
		}
		return true;
	}

	public static void DrawText(string text)
	{
		Vector4 black = ImGuiColor.Float4.Black;
		Vector2 cursorPos = ImGui.GetCursorPos();
		ImGui.SetCursorPos(cursorPos - new Vector2(-1f, -1f));
		ImGui.PushStyleColor(ImGuiCol.Text, black);
		ImGui.TextUnformatted(text);
		ImGui.PopStyleColor();
		ImGui.SetCursorPos(cursorPos);
		ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColor.Float4.White);
		ImGui.TextUnformatted(text);
		ImGui.PopStyleColor();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SameLine()
	{
		ImGui.SameLine();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Text(string text)
	{
		ImGui.Text(text);
	}

	public static void DrawTextInWorld(string title, Vector3d position, string text1, Vector4 colorText)
	{
		DrawText(title, WorldToScreen(position) - ImGui.GetWindowSize() * 0.5f, text1, colorText);
	}

	public static void DrawTextInWorld(string title, Vector3 position, string text1, Vector4 colorText)
	{
		DrawText(title, WorldToScreen(position) - ImGui.GetWindowSize() * 0.5f, text1, colorText);
	}

	public static void DrawText(string title, Vector2 screenPosition, string text1, Vector4 colorText)
	{
		if (!string.IsNullOrEmpty(text1))
		{
			ImGui.Begin(title, (ImGuiWindowFlags)799685);
			ImGui.SetWindowPos(screenPosition, ImGuiCond.Always);
			Vector4 black = ImGuiColor.Float4.Black;
			Vector2 cursorPos = ImGui.GetCursorPos();
			ImGui.SetCursorPos(cursorPos - new Vector2(-1f, -1f));
			ImGui.PushStyleColor(ImGuiCol.Text, black);
			ImGui.TextUnformatted(text1);
			ImGui.PopStyleColor();
			ImGui.SetCursorPos(cursorPos);
			ImGui.PushStyleColor(ImGuiCol.Text, colorText);
			ImGui.TextUnformatted(text1);
			ImGui.PopStyleColor();
			ImGui.End();
		}
	}

	public static Vector2 WorldToScreen(Vector3d position)
	{
		return WorldToScreen(position.ToVector3());
	}

	public static Vector2 WorldToScreen(Vector3 position)
	{
		Vector3 vector = CameraController.CurrentCamera.WorldToScreenPoint(position);
		vector.y = 0f - (vector.y - (float)Screen.height);
		return vector;
	}

	public static void SetCursorForCenter(float widthOfElement)
	{
		SetCursorPosX(ScreenCenter.x - widthOfElement / 2f);
	}

	public static void TextShadow(string text, uint color, TextAlignment alignment = TextAlignment.Left, bool unformatted = false)
	{
		Vector2 cursorPos = ImGui.GetCursorPos();
		ImGui.SetCursorPos(cursorPos - new Vector2(-1f, -1f));
		ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColor.Float4.Black);
		if (unformatted)
		{
			ImGui.TextUnformatted(text);
		}
		else
		{
			ImGuiUn.Text(text, alignment);
		}
		ImGui.PopStyleColor();
		ImGui.SetCursorPos(cursorPos);
		ImGui.PushStyleColor(ImGuiCol.Text, color);
		if (unformatted)
		{
			ImGui.TextUnformatted(text);
		}
		else
		{
			ImGuiUn.Text(text, alignment);
		}
		ImGui.PopStyleColor();
	}

	public static ImFontPtr GetFont(int i)
	{
		return ImGui.GetIO().Fonts.Fonts[i];
	}

	public static void SetCurrentWindowToCenter(ImGuiCond condition = ImGuiCond.Always)
	{
		ImGui.SetWindowPos(ScreenCenter - ImGui.GetWindowSize() / 2f, condition);
	}

	public static bool IsMouseWithinWindow()
	{
		Vector2 position = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
		Vector2 contentRegionAvail = ImGui.GetContentRegionAvail();
		return new Rect(position, contentRegionAvail).Contains(ImGui.GetMousePos());
	}

	public static bool BeginTabItem(Assets.Scripts.Localization2.GameString gameString)
	{
		return ImGui.BeginTabItem(gameString.DisplayString);
	}

	public static void SetCursorPosX(float val)
	{
		ImGui.SetCursorPosX(val);
	}

	public static uint ColorConvertFloat4ToU32(Vector4 v4)
	{
		return ImGui.ColorConvertFloat4ToU32(v4);
	}

	public static void Checkbox(string text, ref bool @checked)
	{
		ImGui.Checkbox(text, ref @checked);
	}

	public static void ShowTooltip(string tooltip, uint color = 0u)
	{
		if (color != 0)
		{
			ImGui.PushStyleColor(ImGuiCol.Text, color);
		}
		ImGui.BeginTooltip();
		ImGui.Text(tooltip);
		ImGui.EndTooltip();
		if (color != 0)
		{
			ImGui.PopStyleColor();
		}
	}
}
