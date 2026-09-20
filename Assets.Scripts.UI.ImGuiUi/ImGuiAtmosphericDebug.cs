using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Util;
using ImGuiNET;
using Networks;
using UI.ImGuiUi;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public static class ImGuiAtmosphericDebug
{
	public static bool PipeNetworkDebugOverlay = false;

	public static bool LandingPadNetworkDebugOverlay = false;

	public static bool DebugRoomAtmosphereOverlay = false;

	public static bool DebugWorldAtmosphereOverlay = false;

	public static bool DebugLiquidAtmosphereOverlay = false;

	public static bool DebugGlobalAtmosphereOverlay = false;

	public static bool DebugThingAtmosphereOverlay = false;

	public static bool DebugWorldAtmosphereDirectionOverlay = false;

	private static readonly Action<Atmosphere> DebugAtmosphereAction = delegate(Atmosphere atmosphere)
	{
		if (atmosphere != null && !atmosphere.BeingDestroyed)
		{
			if (DebugLiquidAtmosphereOverlay && (atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World || atmosphere.Room != null))
			{
				if (atmosphere.TotalMolesLiquids > MoleQuantity.Zero)
				{
					ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.Blue;
					float num = atmosphere.LiquidVolumeRatio * 2f;
					Vector3 vector = new Vector3(2f, num, 2f);
					Vector3 vector2 = new Vector3(0f, -1f + num / 2f, 0f);
					ImGuiExtensions.Rendering.DrawCube(atmosphere.WorldPosition + vector2, atmosphere.WorldRotation * vector);
				}
				string text = $"Id: {ToString(atmosphere.ReferenceId)} [{atmosphere.Grid.x}, {atmosphere.Grid.y}, {atmosphere.Grid.z}]" + "\nPressure: " + ToString(atmosphere.PressureGassesAndLiquids.ToFloat()) + " KPa\nTemperature: " + ToString(atmosphere.Temperature.ToFloat()) + " K" + $"\nVolume: {atmosphere.TotalVolumeLiquids.ToFloat()} / {ToString(atmosphere.Volume.ToFloat())} L" + "\n" + atmosphere.GasMixture.DebugPrint();
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.White;
				ImGuiExtensions.Rendering.DrawTextInWorld(text, atmosphere.WorldPosition, (int)atmosphere.ReferenceId);
			}
			if (DebugWorldAtmosphereOverlay && atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World)
			{
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.Red;
				ImGuiExtensions.Rendering.DrawCube(atmosphere.WorldPosition, atmosphere.WorldRotation * RocketGrid.GridSquare);
				string text2 = "ReferenceId: " + ToString(atmosphere.ReferenceId) + "\nGrid: " + StringManager.GetGridString(atmosphere.Grid) + "\nPressure: " + ToString(atmosphere.PressureGassesAndLiquids.ToFloat()) + " KPa\nTemperature: " + ToString(atmosphere.Temperature.ToFloat()) + " K\nVolume: " + ToString(atmosphere.Volume.ToFloat()) + " L\nCell: " + StringManager.Get(atmosphere.Cell != null) + "\nDirection: " + StringManager.Get(atmosphere.Direction) + "\n Sparked: " + StringManager.Get(atmosphere.Sparked) + " Inflamed: " + StringManager.Get(atmosphere.Inflamed) + "\n Condensation: " + StringManager.Get(atmosphere.Condensation) + "\nRadiated: " + StringManager.Get(atmosphere.EnergyRadiated) + "\nSolarEnergy: " + StringManager.Get(atmosphere.SolarEnergy) + "\nHasLight: " + StringManager.Get(atmosphere.HasLight);
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.White;
				ImGuiExtensions.Rendering.DrawTextInWorld(text2, atmosphere.WorldPosition, (int)atmosphere.ReferenceId);
			}
			if (DebugWorldAtmosphereDirectionOverlay && atmosphere.Mode == AtmosphereHelper.AtmosphereMode.World)
			{
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.Green;
				ImGuiExtensions.Rendering.DrawClippedLine(ImGuiExtensions.WorldToScreen(atmosphere.WorldPosition), ImGuiExtensions.WorldToScreen(atmosphere.WorldPosition + atmosphere.Direction * 0.05f));
			}
			if (DebugRoomAtmosphereOverlay && atmosphere.Room != null)
			{
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.Yellow;
				ImGuiExtensions.Rendering.DrawCube(atmosphere.WorldPosition, atmosphere.WorldRotation * RocketGrid.GridSquare);
				string text3 = "ReferenceId: " + ToString(atmosphere.ReferenceId) + "\nPressure: " + ToString(atmosphere.PressureGassesAndLiquids.ToFloat()) + " KPa\nTemperature: " + ToString(atmosphere.Temperature.ToFloat()) + " K\nVolume: " + ToString(atmosphere.Volume.ToFloat()) + " L\nDirection: " + StringManager.Get(atmosphere.Direction) + "\nRoomId: " + ToString(atmosphere.Room.RoomId) + $"\nRoomType: {atmosphere.Room.RoomType}" + "\nGridCount: " + ToString(atmosphere.Room.Grids.Count) + "\nDeletionCandidate: " + atmosphere.Room.IsDeletionCandidate;
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.White;
				ImGuiExtensions.Rendering.DrawTextInWorld(text3, atmosphere.WorldPosition, (int)atmosphere.ReferenceId);
			}
			if (DebugThingAtmosphereOverlay && atmosphere.Mode == AtmosphereHelper.AtmosphereMode.Thing && atmosphere.Thing != null && (atmosphere.Thing.RootParent == atmosphere.Thing || atmosphere.Thing is ISuit))
			{
				string text4 = "ReferenceId: " + ToString(atmosphere.ReferenceId) + "\n Thing: " + atmosphere.Thing.DisplayName + "\n Thing Id: " + ToString(atmosphere.Thing.ReferenceId) + "\n \nPressure: " + ToString(atmosphere.PressureGassesAndLiquids.ToFloat()) + " KPa\nTemperature: " + ToString(atmosphere.Temperature.ToFloat()) + " K\nVolume: " + ToString(atmosphere.Volume.ToFloat()) + " L\nEnergy Radiated: " + ToString(atmosphere.Thing.EnergyRadiated) + " J\nEnergy Convected: " + ToString(atmosphere.Thing.EnergyConvected) + " J\nSolarHeating: " + ToString(atmosphere.SolarEnergyReceived) + " J\nTotalMoles: " + ToString(atmosphere.TotalMoles.ToFloat());
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.White;
				ImGuiExtensions.Rendering.DrawTextInWorld(text4, atmosphere.Thing.Position, (int)atmosphere.ReferenceId);
			}
		}
	};

	public static void DrawAction()
	{
		try
		{
			if (PipeNetworkDebugOverlay)
			{
				DensePool<PipeNetwork>.ActiveEnumerable.Enumerator enumerator = PipeNetwork.AllPipeNetworks.Active().GetEnumerator();
				while (enumerator.MoveNext())
				{
					enumerator.Current.OnImGuiDraw();
				}
			}
			if (LandingPadNetworkDebugOverlay)
			{
				foreach (LandingPadNetwork allLandingPadNetwork in LandingPadNetwork.AllLandingPadNetworks)
				{
					allLandingPadNetwork.OnImGuiDraw();
				}
			}
			if (DebugWorldAtmosphereOverlay || DebugRoomAtmosphereOverlay || DebugGlobalAtmosphereOverlay || DebugThingAtmosphereOverlay || DebugWorldAtmosphereDirectionOverlay || DebugLiquidAtmosphereOverlay)
			{
				DebugAtmosphere();
			}
			if (DebugLiquidAtmosphereOverlay)
			{
				DebugLiquidSolver();
			}
		}
		catch
		{
		}
	}

	private static string ToString(int integer)
	{
		return StringManager.Get(integer);
	}

	private static string ToString(long refID)
	{
		return StringManager.Get(refID);
	}

	private static string ToString(float val)
	{
		return StringManager.Get(val);
	}

	private static void DebugLiquidSolver()
	{
		for (int num = LiquidSolver.Instance.ActiveParticles.Count - 1; num >= 0; num--)
		{
			ImGuiExtensions.Rendering.DrawCube(LiquidSolver.Instance.ActiveParticles[num].Transform.position, Vector3.one * 0.1f);
		}
		ImGui.Begin("liquiddebug", (ImGuiWindowFlags)799685);
		ImGui.SetWindowPos(new Vector2(10f, 10f), ImGuiCond.Always);
		string text = StringManager.Get(LiquidSolver.Instance.ActiveParticles.Count);
		string text2 = StringManager.Get(LiquidSolver.Instance.MaxParticles);
		ImguiHelper.DrawText("Liquid Solver Info:");
		ImguiHelper.DrawText("Particles: " + text + " / " + text2);
		ImGui.End();
	}

	private static void DebugAtmosphere()
	{
		AtmosphericsManager.AllAtmospheres.ForEach(DebugAtmosphereAction);
		if (DebugThingAtmosphereOverlay)
		{
			RadiatorRotatable.DrawDebug();
		}
	}
}
