using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Util;
using ImGuiNET;
using UI.ImGuiUi;
using UnityEngine;

namespace TerrainSystem;

public static class RegionManager
{
	public static List<RegionSet> RegionSets;

	public static bool DoDrawDebug;

	public static void LoadRegionSets(WorldSettingData worldSettingData)
	{
		RegionSets = worldSettingData.RegionSets;
	}

	public static void ClearAll()
	{
		RegionSets = null;
	}

	public static bool TryGetRegionsAtWorldPosition(Vector3 worldPosition, List<(RegionSet set, Region region)> regionPairs)
	{
		if (RegionSets == null)
		{
			return false;
		}
		if (RegionSets.Count == 0)
		{
			return false;
		}
		foreach (RegionSet regionSet in RegionSets)
		{
			if (TryGetRegionAtWorldPosition(regionSet, worldPosition, out var region))
			{
				regionPairs.Add((regionSet, region));
			}
		}
		return true;
	}

	public static bool TryGetRegionsAtWorldPosition(Vector3 worldPosition, List<Region> regions)
	{
		if (RegionSets == null)
		{
			return false;
		}
		if (RegionSets.Count == 0)
		{
			return false;
		}
		foreach (RegionSet regionSet in RegionSets)
		{
			if (TryGetRegionAtWorldPosition(regionSet, worldPosition, out var region))
			{
				regions.Add(region);
			}
		}
		return true;
	}

	public static bool TryGetRegionAtWorldPosition(RegionSet regionSet, Vector3 worldPosition, out Region region)
	{
		if (!regionSet.IsValid())
		{
			ConsoleWindow.PrintError("Region set texture is null for " + regionSet.Id);
			region = null;
			return false;
		}
		int size = VoxelConstants.Size;
		int textureWidth = regionSet.TextureWidth;
		Vector3Int vector3Int = ((worldPosition + VoxelConstants.OriginOffsetInt) / size * textureWidth).RoundToInt();
		Color pixel = regionSet.GetPixel(vector3Int.x, vector3Int.z);
		return regionSet.TryGetRegionFromColor(pixel, out region);
	}

	public static void DrawDebug()
	{
		if (!DoDrawDebug || GameManager.GameState != GameState.Running)
		{
			return;
		}
		Vector3 worldPosition = InventoryManager.ParentHuman?.Position ?? Vector3.zero;
		List<(RegionSet, Region)> list = new List<(RegionSet, Region)>();
		bool num = TryGetRegionsAtWorldPosition(worldPosition, list);
		ImGui.Begin("RegionDebug", (ImGuiWindowFlags)799685);
		ImGui.SetWindowPos(new Vector2(10f, 10f), ImGuiCond.Always);
		if (num)
		{
			foreach (var item in list)
			{
				ImguiHelper.DrawText("Set: " + item.Item1.Id + " Region: " + item.Item2.Id);
			}
		}
		else
		{
			ImguiHelper.DrawText("No regions found");
		}
		ImGui.End();
	}

	public static bool EvaluateRegionsAtPosition(Vector3 clusterCenterPosition, int idHash)
	{
		if (RegionSets == null)
		{
			return false;
		}
		if (RegionSets.Count == 0)
		{
			return false;
		}
		foreach (RegionSet regionSet in RegionSets)
		{
			if (TryGetRegionAtWorldPosition(regionSet, clusterCenterPosition, out var region) && region.IdHash == idHash)
			{
				return true;
			}
		}
		return false;
	}
}
