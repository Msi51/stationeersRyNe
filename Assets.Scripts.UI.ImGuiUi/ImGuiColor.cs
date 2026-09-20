using ImGuiNET;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public static class ImGuiColor
{
	public static class Float4
	{
		public static Vector4 DefaultColor = new Vector4(0.7f, 0.7f, 0.7f, 1f);

		public static Vector4 Grey = new Vector4(0.4f, 0.4f, 0.4f, 1f);

		public static Vector4 DarkGrey = new Vector4(0.1f, 0.1f, 0.1f, 1f);

		public static Vector4 Red = new Vector4(1f, 0f, 0f, 1f);

		public static Vector4 LightRedTransparent = new Vector4(1f, 0.3f, 0.3f, 1f);

		public static Vector4 DarkRed = new Vector4(0.6f, 0f, 0f, 1f);

		public static Vector4 Black = new Vector4(0f, 0f, 0f, 1f);

		public static Vector4 Yellow = new Vector4(1f, 1f, 0f, 1f);

		public static Vector4 YellowTransparent = new Vector4(1f, 0.5f, 0f, 0.4f);

		public static Vector4 Blue = new Vector4(0f, 0.3f, 1f, 1f);

		public static Vector4 BlueTransparent = new Vector4(0f, 0.3f, 1f, 0.4f);

		public static Vector4 Green = new Vector4(0f, 1f, 0f, 1f);

		public static Vector4 GreenTransparent = new Vector4(0f, 1f, 0f, 0.4f);

		public static Vector4 HalfTransparent = new Vector4(1f, 1f, 1f, 0.5f);

		public static Vector4 Transparent = new Vector4(0f, 0f, 0f, 0f);

		public static Vector4 LightGreen = new Vector4(0.5f, 1f, 0f, 1f);

		public static Vector4 Cyan = new Vector4(0f, 1f, 1f, 1f);

		public static Vector4 Magenta = new Vector4(1f, 0f, 1f, 1f);

		public static Vector4 White = new Vector4(1f, 1f, 1f, 1f);

		public static Vector4 WhiteTransparent = new Vector4(1f, 1f, 1f, 0.6f);

		public static Vector4 DarkGreyTransparent = new Vector4(0.1f, 0.1f, 0.1f, 0.4f);

		public static Vector4 LightBlue = new Vector4(22f / 85f, 40f / 51f, 49f / 51f, 1f);

		public static Vector4 GreyTransparent = new Vector4(0.4f, 0.4f, 0.4f, 0.4f);

		public static Vector4 Orange = new Vector4(1f, 0.64705884f, 0f, 1f);

		public static Vector4 LightBrown = new Vector4(40f / 51f, 0.5019608f, 0.22745098f, 1f);

		public static Vector4 MustardYellow = new Vector4(0.827451f, 37f / 51f, 0.23529412f, 1f);

		public static Vector4 Violet = new Vector4(0.70980394f, 0.3254902f, 78f / 85f, 1f);

		public static Vector4 SoftRose = new Vector4(0.85f, 0.55f, 0.6f, 1f);

		public static Vector4 SoftSage = new Vector4(0.55f, 0.78f, 0.58f, 1f);

		public static Vector4 SoftSky = new Vector4(0.55f, 0.78f, 0.92f, 1f);

		public static Vector4 SoftMustard = new Vector4(0.86f, 0.74f, 0.4f, 1f);

		public static Vector4 SoftLilac = new Vector4(0.78f, 0.62f, 0.9f, 1f);

		public static Vector4 SoftCopper = new Vector4(0.82f, 0.6f, 0.4f, 1f);

		public static Vector4 SoftPeach = new Vector4(0.95f, 0.72f, 0.55f, 1f);

		public static Vector4 SoftMint = new Vector4(0.62f, 0.85f, 0.78f, 1f);

		public static Vector4 Border => ImGuiExtensions.GetFontStyle().Colors[5];

		public static Vector4 TabHovered => ImGuiExtensions.GetFontStyle().Colors[34];

		public static Vector4 PlotHistogramHovered => ImGuiExtensions.GetFontStyle().Colors[41];
	}

	public static class Integer
	{
		public static uint DefaultColor = ImGui.ColorConvertFloat4ToU32(Float4.DefaultColor);

		public static uint White = ImGui.ColorConvertFloat4ToU32(Float4.White);

		public static uint WhiteTransparent = ImGui.ColorConvertFloat4ToU32(Float4.WhiteTransparent);

		public static uint Black = ImGui.ColorConvertFloat4ToU32(Float4.Black);

		public static uint Grey = ImGui.ColorConvertFloat4ToU32(Float4.Grey);

		public static uint DarkGrey = ImGui.ColorConvertFloat4ToU32(Float4.DarkGrey);

		public static uint Yellow = ImGui.ColorConvertFloat4ToU32(Float4.Yellow);

		public static uint YellowTransparent = ImGui.ColorConvertFloat4ToU32(Float4.YellowTransparent);

		public static uint GreenTransparent = ImGui.ColorConvertFloat4ToU32(Float4.GreenTransparent);

		public static uint Red = ImGui.ColorConvertFloat4ToU32(Float4.Red);

		public static uint LightRedTransparent = ImGui.ColorConvertFloat4ToU32(Float4.LightRedTransparent);

		public static uint Green = ImGui.ColorConvertFloat4ToU32(Float4.Green);

		public static uint Blue = ImGui.ColorConvertFloat4ToU32(Float4.Blue);

		public static uint LightBlue = ImGui.ColorConvertFloat4ToU32(Float4.LightBlue);

		public static uint BlueTransparent = ImGui.ColorConvertFloat4ToU32(Float4.BlueTransparent);

		public static uint DarkGreyTransparent = ImGui.ColorConvertFloat4ToU32(Float4.DarkGreyTransparent);

		public static uint GreyTransparent = ImGui.ColorConvertFloat4ToU32(Float4.GreyTransparent);

		public static uint HalfTransparent = ImGui.ColorConvertFloat4ToU32(Float4.HalfTransparent);

		public static uint Transparent = ImGui.ColorConvertFloat4ToU32(Float4.Transparent);

		public static uint Orange = ImGui.ColorConvertFloat4ToU32(Float4.Orange);

		public static uint Magenta = ImGui.ColorConvertFloat4ToU32(Float4.Magenta);

		public static uint LightGreen = ImGui.ColorConvertFloat4ToU32(Float4.LightGreen);

		public static uint LightBrown = ImGui.ColorConvertFloat4ToU32(Float4.LightBrown);

		public static uint MustardYellow = ImGui.ColorConvertFloat4ToU32(Float4.MustardYellow);

		public static uint Violet = ImGui.ColorConvertFloat4ToU32(Float4.Violet);

		public static uint SoftRose = ImGui.ColorConvertFloat4ToU32(Float4.SoftRose);

		public static uint SoftSage = ImGui.ColorConvertFloat4ToU32(Float4.SoftSage);

		public static uint SoftSky = ImGui.ColorConvertFloat4ToU32(Float4.SoftSky);

		public static uint SoftMustard = ImGui.ColorConvertFloat4ToU32(Float4.SoftMustard);

		public static uint SoftLilac = ImGui.ColorConvertFloat4ToU32(Float4.SoftLilac);

		public static uint SoftCopper = ImGui.ColorConvertFloat4ToU32(Float4.SoftCopper);

		public static uint SoftPeach = ImGui.ColorConvertFloat4ToU32(Float4.SoftPeach);

		public static uint SoftMint = ImGui.ColorConvertFloat4ToU32(Float4.SoftMint);

		public static uint TabHovered => ImGui.ColorConvertFloat4ToU32(Float4.TabHovered);

		public static uint Border => ImGui.ColorConvertFloat4ToU32(Float4.Border);
	}
}
