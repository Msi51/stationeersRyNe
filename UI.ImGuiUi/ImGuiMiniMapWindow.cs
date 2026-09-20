using System;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using ImGuiNET;
using Objects.Structures;
using TerrainSystem;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;

namespace UI.ImGuiUi;

public class ImGuiMiniMapWindow : UI.ImGuiUi.ImGuiWindows.ImGuiWindow
{
	public static bool IsShowGeysers;

	public static bool IsRegionOverlay;

	public static bool IsShowSpawnPoints;

	public static int SelectedOverlay;

	public static ImGuiMiniMapWindow Window = new ImGuiMiniMapWindow();

	public const string MINI_MAP_WINDOW_TITLE = "MiniMap Window";

	public Texture2D MapTexture;

	public Vector3? Target;

	public const int MapTexSize = 512;

	public override bool MouseControlMode => false;

	public override bool CaptureKeyInput => false;

	public override ImGuiWindowFlags Flags => (ImGuiWindowFlags)264;

	public bool MapHasRegions
	{
		get
		{
			WorldSetting current = WorldSetting.Current;
			if (current == null)
			{
				return false;
			}
			return current.Data?.RegionSets.Count > 0;
		}
	}

	public ImGuiMiniMapWindow()
		: base("MiniMap Window", new Vector2(528f, 1000f))
	{
	}

	public Texture2D SelectedRegionTexture()
	{
		if (!MapHasRegions)
		{
			return null;
		}
		return WorldSetting.Current?.Data?.RegionSets[SelectedOverlay].TextureReference?.Texture;
	}

	public override void OnOpen()
	{
		MapTexture = WorldSetting.Current.Data.TerrainSettings.MiniMapTexture.Texture;
	}

	public override void OnClose()
	{
		MapTexture = null;
	}

	public override void DrawContent()
	{
		if ((object)InventoryManager.Parent == null)
		{
			return;
		}
		ImGui.Text(WorldSetting.Current.Name);
		ImGui.Separator();
		Vector2 cursorPos = ImGui.GetCursorPos();
		if ((object)MapTexture != null)
		{
			ImGui.Image((IntPtr)GetTextureId(MapTexture), new Vector2(512f, 512f));
		}
		if (MapHasRegions && IsRegionOverlay)
		{
			ImGui.SetCursorPos(cursorPos);
			ImGui.Image((IntPtr)GetTextureId(SelectedRegionTexture()), new Vector2(512f, 512f), new Vector4(1f, 1f, 1f, 0.6f));
		}
		if (Target.HasValue && ImGui.Button("Teleport to Target"))
		{
			Vector3 safePoint = SpawnPoint.GetSafePoint(Target.Value, Vector3.up * 10f);
			InventoryManager.Parent.Transform.position = safePoint;
		}
		if (InputMouse.IsMouseControl)
		{
			Vector2 mousePos = ImGui.GetMousePos();
			Vector2 vector = cursorPos + ImGui.GetWindowPos();
			if (mousePos.x >= vector.x && mousePos.x < vector.x + 512f && mousePos.y >= vector.y && mousePos.y < vector.y + 512f)
			{
				Vector3 vector2 = new Vector3((mousePos.x - vector.x) / 512f * (float)VoxelConstants.Size - (float)VoxelConstants.Size / 2f, 1f, (1f - (mousePos.y - vector.y) / 512f) * (float)VoxelConstants.Size - (float)VoxelConstants.Size / 2f);
				if (Input.GetKeyDown(KeyCode.Mouse0))
				{
					Target = vector2;
				}
				if (Input.GetKeyDown(KeyCode.Mouse1))
				{
					Target = null;
				}
				ImGui.BeginTooltip();
				ImGui.Text("X: ");
				ImGui.SameLine();
				ImGui.Text(StringManager.Get(vector2.x));
				ImGui.Text("Y: ");
				ImGui.SameLine();
				ImGui.Text(StringManager.Get(vector2.z));
				if (IsRegionOverlay && RegionManager.TryGetRegionAtWorldPosition(WorldSetting.Current.Data.RegionSets[SelectedOverlay], vector2, out var region))
				{
					if (PointOfInterestManager.GetPointOfInterest(region, out var poi))
					{
						ImGui.Text("POI: ");
						ImGui.SameLine();
						ImGui.Text(poi.Name);
					}
					ImGui.Text(region.Id);
				}
				ImGui.EndTooltip();
			}
		}
		ImGui.Separator();
		ImGui.Text("X: ");
		ImGui.SameLine();
		ImGui.Text(StringManager.Get(InventoryManager.Parent.Position.x));
		ImGui.Text("Y: ");
		ImGui.SameLine();
		ImGui.Text(StringManager.Get(InventoryManager.Parent.Position.z));
		if (IsRegionOverlay)
		{
			ImGui.Text("Current Region: ");
			ImGui.SameLine();
			RegionManager.TryGetRegionAtWorldPosition(WorldSetting.Current.Data.RegionSets[SelectedOverlay], InventoryManager.Parent.Position, out var region2);
			ImGui.Text((region2?.Name != null) ? region2.Name.ToString() : (region2?.Id ?? "Unknown"));
		}
		ImGui.Separator();
		ImGui.Checkbox("Display Geysers", ref IsShowGeysers);
		ImGui.Checkbox("Region Overlay", ref IsRegionOverlay);
		ImGui.Checkbox("Show Spawn Points", ref IsShowSpawnPoints);
		if (MapHasRegions && IsRegionOverlay)
		{
			ImguiHelper.Draw(WorldSetting.Current.Data.RegionSets[SelectedOverlay].GetName(), ref SelectedOverlay, WorldSetting.Current.Data.RegionSets);
		}
		DrawTextOnMinimap(InventoryManager.Parent.Position, cursorPos, "X", ImGuiColor.Float4.Red);
		if (Target.HasValue)
		{
			DrawTextOnMinimap(Target.Value, cursorPos, "T", ImGuiColor.Float4.Blue);
		}
		if (IsShowGeysers)
		{
			foreach (Geyser allGeyser in Geyser.AllGeysers)
			{
				DrawTextOnMinimap(allGeyser.Position, cursorPos, "G", ImGuiColor.Float4.Green);
			}
		}
		if (!IsShowSpawnPoints)
		{
			return;
		}
		foreach (StartLocationData startLocationData2 in WorldSetting.Current.Data.StartLocationDatas)
		{
			StartLocationData startLocationData = DataCollection.Get<StartLocationData>(startLocationData2.Id);
			if (!(startLocationData is RoundRobinStartLocationData))
			{
				DrawTextOnMinimap(startLocationData.WorldPosition(), cursorPos, "S", ImGuiColor.Float4.Yellow);
			}
		}
	}

	private void DrawTextOnMinimap(Vector3 worldPosition, Vector2 cursorPos, string text, Vector4 color)
	{
		ImGui.SetCursorPos(GetTextMapPos(worldPosition, cursorPos, text));
		ImGui.TextColored(color, text);
	}

	private Vector2 GetTextMapPos(Vector3 worldPosition, Vector2 cursorPos, string text)
	{
		Vector2 vector = ImGui.CalcTextSize(text) * 0.5f;
		float num = 512f / (float)VoxelConstants.Size;
		Vector3 vector2 = worldPosition + new Vector3(VoxelConstants.Offset, 0f, VoxelConstants.Offset);
		return new Vector2(cursorPos.x + vector2.x * num, cursorPos.y + ((float)VoxelConstants.Size - vector2.z) * num) - vector;
	}

	private static int GetTextureId(Texture texture)
	{
		return ImGuiManager.igTextureManager.GetTextureId(texture);
	}
}
