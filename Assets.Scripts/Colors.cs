using ImGuiNET;
using UnityEngine;

namespace Assets.Scripts;

public static class Colors
{
	public static class CGA
	{
		private const float hx00 = 0f;

		private const float hx55 = 0.3125f;

		private const float hxAA = 0.625f;

		private const float hxFF = 1f;

		public static uint Black => ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 1f));

		public static uint Blue => ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0.625f, 1f));

		public static uint Green => ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0.625f, 0f, 1f));

		public static uint Cyan => ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0.625f, 0.625f, 1f));

		public static uint Red => ImGui.ColorConvertFloat4ToU32(new Vector4(0.625f, 0f, 0f, 1f));

		public static uint Magenta => ImGui.ColorConvertFloat4ToU32(new Vector4(0.625f, 0f, 0.625f, 1f));

		public static uint Brown => ImGui.ColorConvertFloat4ToU32(new Vector4(0.625f, 0.3125f, 0f, 1f));

		public static uint Gray => ImGui.ColorConvertFloat4ToU32(new Vector4(0.625f, 0.625f, 0.625f, 1f));

		public static uint DarkGray => ImGui.ColorConvertFloat4ToU32(new Vector4(0.3125f, 0.3125f, 0.3125f, 1f));

		public static uint LightBlue => ImGui.ColorConvertFloat4ToU32(new Vector4(0.3125f, 0.3125f, 1f, 1f));

		public static uint LightGreen => ImGui.ColorConvertFloat4ToU32(new Vector4(0.3125f, 1f, 0.3125f, 1f));

		public static uint LightCyan => ImGui.ColorConvertFloat4ToU32(new Vector4(0.3125f, 1f, 1f, 1f));

		public static uint LightRed => ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.3125f, 0.3125f, 1f));

		public static uint LightMagenta => ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.3125f, 1f, 1f));

		public static uint Yellow => ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 0.3125f, 1f));

		public static uint White => ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
	}

	public static class LCARS
	{
	}

	public static class StationeersCommon
	{
	}

	public static uint Transparent => ImGui.ColorConvertFloat4ToU32(Vector4.zero);
}
