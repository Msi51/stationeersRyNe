using System.Collections.Generic;
using System.Globalization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using ImGuiNET;
using UI.ImGuiUi;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public static class WorldSettingToolsImguiWindow
{
	public static bool Show = false;

	private static string _volumeString;

	private const string SolarAngleTemperatureTitle = "SolarAngleTemperature";

	private const string SolarDistanceTemperatureOffsetDayTitle = "SolarRadiationOffsetDay";

	private const string SolarDistanceTemperatureOffsetNightTitle = "SolarRadiationOffsetNight";

	private const string GHGTemperatureOffsetDayTitle = "GHGTemperatureOffsetDay";

	private const string GHGTemperatureOffsetNightTitle = "GHGTemperatureOffsetNight";

	private const string DensityOffsetDayTitle = "DensityOffsetDay";

	private const string DensityOffsetNightTitle = "DensityOffsetNight";

	private const string AggregateTemperatureTitle = "AggregateTemperature";

	private const int SOLAR_ANGLE_POINTS = 181;

	private const int OFFSET_POINTS = 100;

	public static GraphWithKeys SolarAngleTemperature;

	public static GraphWithKeys SolarDistanceTemperatureOffsetDay;

	public static GraphWithKeys SolarDistanceTemperatureOffsetNight;

	public static GraphWithKeys GHGTemperatureOffsetDay;

	public static GraphWithKeys GHGTemperatureOffsetNight;

	public static GraphWithKeys DensityOffsetDay;

	public static GraphWithKeys DensityOffsetNight;

	public static GraphElement AggregateTemperature = new GraphElement("AggregateTemperature", 181, ImGuiColor.Float4.Magenta);

	public static List<GraphWithKeys> GHGIndexes = new List<GraphWithKeys>();

	public static WorldSettingData WorkingData { get; set; }

	public static void DrawOption()
	{
		if (!Show)
		{
			SetInputEnabled(enabled: true);
		}
		else
		{
			SetInputEnabled(enabled: false);
		}
		Show = !Show;
		_ = Show;
	}

	private static void SetInputEnabled(bool enabled)
	{
		SetInputKeyState(enabled);
		InputMouse.SetMouseControl(enabled);
	}

	private static void SetInputKeyState(bool isTyping)
	{
		string key = "InputWindow_WorldSettingTools";
		if (isTyping)
		{
			KeyManager.SetInputState(key, KeyInputState.Typing);
		}
		else
		{
			KeyManager.RemoveInputState(key);
		}
	}

	public static void Draw()
	{
		ImGuiWindowFlags flags = (ImGuiWindowFlags)19712;
		bool show = Show;
		ImGui.Begin("GlobalTemperature", ref Show, flags);
		if (show && !Show)
		{
			SetInputEnabled(enabled: false);
		}
		if (ImGui.IsWindowFocused() && KeyManager.GetButton(KeyCode.Escape))
		{
			if (Show)
			{
				SetInputEnabled(enabled: false);
			}
			Show = false;
			ImGui.End();
			return;
		}
		ImGui.SetWindowSize(new Vector2(1800f, 1000f), ImGuiCond.Once);
		if (ImGui.Button("Populate"))
		{
			Populate();
		}
		if (WorkingData == null)
		{
			return;
		}
		ImguiHelper.DrawInput("WorldId", ref WorkingData.Id);
		if (ImGui.Button("ExportWorldSetting"))
		{
			WorldSettingData.SaveWorldSetting(WorkingData.Id, WorkingData);
		}
		if (ImGui.Button("ExportGHGGraphs"))
		{
			TerraForming.ExportGhgGraphs();
		}
		if (ImGui.Button("Apply"))
		{
			Apply();
		}
		ImGui.Separator();
		if (ImGui.BeginTabBar("Tabs", (ImGuiTabBarFlags)40))
		{
			if (ImGui.BeginTabItem("GlobalTemperature"))
			{
				ImGui.Text("Average Global Temperature: ");
				ImGui.SameLine();
				ImGui.Text(PlanetaryAtmosphereSimulation.GetGlobalAverageTemperature().ToFloat().ToString(CultureInfo.InvariantCulture));
				ImGui.Separator();
				ImGui.Columns(2, "data", border: false);
				ImGui.SetColumnWidth(0, 500f * ImguiHelper.UIScale);
				ImGui.BeginChild("Data");
				ImGui.Separator();
				ImGui.GetCursorPos();
				SolarAngleTemperature.DrawGraphToggle();
				SolarDistanceTemperatureOffsetDay.DrawGraphToggle();
				SolarDistanceTemperatureOffsetNight.DrawGraphToggle();
				GHGTemperatureOffsetDay.DrawGraphToggle();
				GHGTemperatureOffsetNight.DrawGraphToggle();
				DensityOffsetDay.DrawGraphToggle();
				DensityOffsetNight.DrawGraphToggle();
				AggregateTemperature.DrawSetting();
				ImGui.Separator();
				if (SolarAngleTemperature.GraphElement.Visible)
				{
					SolarAngleTemperature.DrawKeyFrames();
				}
				if (SolarDistanceTemperatureOffsetDay.GraphElement.Visible)
				{
					SolarDistanceTemperatureOffsetDay.DrawKeyFrames();
				}
				if (SolarDistanceTemperatureOffsetNight.GraphElement.Visible)
				{
					SolarDistanceTemperatureOffsetNight.DrawKeyFrames();
				}
				if (GHGTemperatureOffsetDay.GraphElement.Visible)
				{
					GHGTemperatureOffsetDay.DrawKeyFrames();
				}
				if (GHGTemperatureOffsetNight.GraphElement.Visible)
				{
					GHGTemperatureOffsetNight.DrawKeyFrames();
				}
				if (DensityOffsetDay.GraphElement.Visible)
				{
					DensityOffsetDay.DrawKeyFrames();
				}
				if (DensityOffsetNight.GraphElement.Visible)
				{
					DensityOffsetNight.DrawKeyFrames();
				}
				ImGui.EndChild();
				ImGui.NextColumn();
				ImGui.Text("Aggregate Temperature: ");
				ImGui.SameLine();
				ImGui.Text(PlanetaryAtmosphereSimulation.AggregateTemperature.ToFloat().ToString(CultureInfo.InvariantCulture));
				ImGui.Text("Solar Angle: ");
				ImGui.SameLine();
				ImGui.Text(PlanetaryAtmosphereSimulation.SolarAngleTemperature.ToFloat().ToString(CultureInfo.InvariantCulture));
				ImGui.Text("Solar Radiation Offset: ");
				ImGui.SameLine();
				ImGui.Text(PlanetaryAtmosphereSimulation.SolarDistanceOffsetTemperature.ToFloat().ToString(CultureInfo.InvariantCulture));
				ImGui.Text("GHG Offset: ");
				ImGui.SameLine();
				ImGui.Text(PlanetaryAtmosphereSimulation.GhgIndexOffset.ToFloat().ToString(CultureInfo.InvariantCulture));
				ImGui.Text("Density Offset: ");
				ImGui.SameLine();
				ImGui.Text(PlanetaryAtmosphereSimulation.DensityOffsetTemperature.ToFloat().ToString(CultureInfo.InvariantCulture));
				ImGui.Text("Latent Offset: ");
				ImGui.SameLine();
				ImGui.Text(PlanetaryAtmosphereSimulation.LatentOffset.ToFloat().ToString(CultureInfo.InvariantCulture));
				ImGui.Text("External Energy Input Offset: ");
				ImGui.SameLine();
				ImGui.Text(PlanetaryAtmosphereSimulation.ExternalInputOffset.ToFloat().ToString(CultureInfo.InvariantCulture));
				Vector2 contentRegionAvail = ImGui.GetContentRegionAvail();
				Vector2 cursorPos = ImGui.GetCursorPos();
				SolarAngleTemperature.DrawGraph(contentRegionAvail, cursorPos);
				SolarDistanceTemperatureOffsetDay.DrawGraph(contentRegionAvail, cursorPos);
				SolarDistanceTemperatureOffsetNight.DrawGraph(contentRegionAvail, cursorPos);
				GHGTemperatureOffsetDay.DrawGraph(contentRegionAvail, cursorPos);
				GHGTemperatureOffsetNight.DrawGraph(contentRegionAvail, cursorPos);
				DensityOffsetDay.DrawGraph(contentRegionAvail, cursorPos);
				DensityOffsetNight.DrawGraph(contentRegionAvail, cursorPos);
				UpdateAggregateTemperature();
				AggregateTemperature.Plot(contentRegionAvail, cursorPos);
				ImGui.Columns(1);
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Global GasMix"))
			{
				ImguiHelper.DrawInput("Volume", ref _volumeString);
				PlanetaryAtmosphereSimulation.Draw();
				if (float.TryParse(_volumeString, out var result) && !RocketMath.Approximately(WorkingData.GlobalAtmosphereData.Volume.Value, result))
				{
					WorkingData.GlobalAtmosphereData.Volume.Value = result;
				}
				WorkingData.GlobalAtmosphereData.GlobalGasMixData.Draw();
				if (WorkingData.GlobalAtmosphereData.GlobalGasMixData.Apply())
				{
					PlanetaryAtmosphereSimulation.RegenerateGlobalFromData(WorkingData.GlobalAtmosphereData);
				}
			}
			if (ImGui.BeginTabItem("Ghg Index Graphs"))
			{
				ImGui.Text("GHG Info");
				GlobalGasMix globalGasMix = PlanetaryAtmosphereSimulation.GetGlobalGasMix();
				Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
				foreach (Chemistry.GasType gasType in values)
				{
					if (gasType != Chemistry.GasType.Undefined && globalGasMix.Get(gasType) > MoleQuantity.Zero)
					{
						DrawGlobalMolesPerLiter(globalGasMix, gasType);
					}
				}
				ImGui.Columns(2, "data", border: false);
				ImGui.SetColumnWidth(0, 500f * ImguiHelper.UIScale);
				ImGui.BeginChild("Data");
				ImGui.Separator();
				foreach (GraphWithKeys gHGIndex in GHGIndexes)
				{
					gHGIndex.DrawGraphToggle();
				}
				ImGui.Separator();
				foreach (GraphWithKeys gHGIndex2 in GHGIndexes)
				{
					if (gHGIndex2.GraphElement.Visible)
					{
						gHGIndex2.DrawKeyFrames();
					}
				}
				ImGui.EndChild();
				ImGui.NextColumn();
				Vector2 contentRegionAvail2 = ImGui.GetContentRegionAvail();
				Vector2 cursorPos2 = ImGui.GetCursorPos();
				foreach (GraphWithKeys gHGIndex3 in GHGIndexes)
				{
					gHGIndex3.DrawGraph(contentRegionAvail2, cursorPos2);
				}
				ImGui.Columns(1);
				ImGui.EndTabItem();
			}
			ImGui.EndTabBar();
		}
		ImGui.End();
	}

	private static void DrawGlobalMolesPerLiter(GlobalGasMix globalGasMix, Chemistry.GasType gasType)
	{
		double milliMolesPerLitre = IdealGas.GetMilliMolesPerLitre(globalGasMix.Volume, globalGasMix.Get(gasType));
		ImGui.Text(EnumCollections.GasTypes.GetName(gasType));
		ImGui.SameLine();
		ImGui.Text(": ");
		ImGui.SameLine();
		ImGui.Text(milliMolesPerLitre.ToString(CultureInfo.InvariantCulture));
		ImGui.SameLine();
		ImGui.Text(" mmol/L");
		if (TerraForming.TerraformingGasCurves.TryGetValue(gasType, out var value))
		{
			ImGui.Text("GHG Index Effect: ");
			ImGui.SameLine();
			ImGui.Text(StringManager.Get(value.Curve.Evaluate((float)milliMolesPerLitre)));
		}
	}

	public static void Apply()
	{
	}

	private static void Populate()
	{
		WorkingData = WorldSetting.Current.Data;
		SolarAngleTemperature = new GraphWithKeys(WorkingData.GlobalAtmosphereData.SolarAngleTemperatureCurveData, "SolarAngleTemperature", 181, ImGuiColor.Float4.Red);
		SolarDistanceTemperatureOffsetDay = new GraphWithKeys(WorkingData.GlobalAtmosphereData.SolarRadiationTemperatureOffset.DayCurveOffsetData, "SolarRadiationOffsetDay", 100, ImGuiColor.Float4.Yellow);
		SolarDistanceTemperatureOffsetNight = new GraphWithKeys(WorkingData.GlobalAtmosphereData.SolarRadiationTemperatureOffset.NightCurveOffsetData, "SolarRadiationOffsetNight", 100, ImGuiColor.Float4.Blue);
		GHGTemperatureOffsetDay = new GraphWithKeys(WorkingData.GlobalAtmosphereData.GHGTemperatureOffset.DayCurveOffsetData, "GHGTemperatureOffsetDay", 200, ImGuiColor.Float4.LightGreen, -100);
		GHGTemperatureOffsetNight = new GraphWithKeys(WorkingData.GlobalAtmosphereData.GHGTemperatureOffset.NightCurveOffsetData, "GHGTemperatureOffsetNight", 200, ImGuiColor.Float4.Green, -100);
		DensityOffsetDay = new GraphWithKeys(WorkingData.GlobalAtmosphereData.DensityOffset.DayCurveOffsetData, "DensityOffsetDay", 100, ImGuiColor.Float4.LightBlue);
		DensityOffsetNight = new GraphWithKeys(WorkingData.GlobalAtmosphereData.DensityOffset.NightCurveOffsetData, "DensityOffsetNight", 100, ImGuiColor.Float4.Grey);
		GHGIndexes.Clear();
		Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
		foreach (Chemistry.GasType key in values)
		{
			if (TerraForming.TerraformingGasCurves.TryGetValue(key, out var value))
			{
				GHGIndexes.Add(new GraphWithKeys(value, "GHG Index " + value.Id, 100, TerraForming.GetGasGraphColor(value.GasType)));
			}
		}
		UpdateAggregateTemperature();
	}

	public static void UpdateAggregateTemperature()
	{
		GlobalGasMix globalGasMix = GlobalGasMix.Create(WorldSetting.Current.Data.GlobalAtmosphereData);
		for (int i = 0; i < 181; i++)
		{
			TemperatureKelvin globalGasMixTemperature = globalGasMix.GetGlobalGasMixTemperature(WorldSetting.Current.Data.GlobalAtmosphereData, i, OrbitalSimulation.System.GetSolarEnergyPercentClamped(OrbitalSimulation.System.GetSolarEnergy(), OrbitalSimulation.System.CalculateSolarIrradiance()));
			AggregateTemperature.Record(globalGasMixTemperature.ToFloat());
		}
	}
}
