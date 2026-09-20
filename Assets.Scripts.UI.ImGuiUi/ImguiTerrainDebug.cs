using Assets.Scripts.Inventory;
using Assets.Scripts.Util;
using TerrainSystem;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public static class ImguiTerrainDebug
{
	public static bool DebugCursorTerrain = false;

	public static bool DebugTerrain = false;

	public const int DEFAULT_DEBUG_SIZE = 16;

	public static int DebugSize = 16;

	public static void DrawDebug()
	{
		try
		{
			if (DebugCursorTerrain)
			{
				CursorTerrainDebug();
			}
			if (DebugTerrain)
			{
				TerrainDebug();
			}
		}
		catch
		{
		}
	}

	private static void CursorTerrainDebug()
	{
		if (!CursorManager.CursorTerrain.IsValid)
		{
			return;
		}
		INode nodeWorldSpace_DEBUG = VoxelTerrain.GetNodeWorldSpace_DEBUG(CursorManager.CursorTerrain.WorldPosition);
		if (nodeWorldSpace_DEBUG != null)
		{
			VoxelNodeType voxelNodeType = (((nodeWorldSpace_DEBUG.NodeType & VoxelNodeType.Crust) != VoxelNodeType.None) ? VoxelNodeType.Crust : (((nodeWorldSpace_DEBUG.NodeType & VoxelNodeType.Dirt) != VoxelNodeType.None) ? VoxelNodeType.Dirt : VoxelNodeType.None));
			switch (voxelNodeType)
			{
			case VoxelNodeType.None:
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.Blue;
				break;
			case VoxelNodeType.Crust:
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.Yellow;
				break;
			case VoxelNodeType.Dirt:
				ImGuiExtensions.Rendering.RenderingColor = ImGuiColor.Integer.Green;
				break;
			}
			int size = nodeWorldSpace_DEBUG.GetSize();
			Vector3 vector = CursorManager.CursorTerrain.WorldPosition + Vector3.one * size * 0.5f;
			ImGuiExtensions.Rendering.DrawCube(vector, Vector3.one);
			ImGuiExtensions.Rendering.DrawTextInWorld("Density: " + StringManager.Get(VoxelTerrain.DensityToFloat(nodeWorldSpace_DEBUG.Density)) + $"\nType: {voxelNodeType}" + "\nSize: " + StringManager.Get(nodeWorldSpace_DEBUG.GetSize()), vector, 3);
		}
	}

	private static void TerrainDebug()
	{
		if (!(InventoryManager.Parent == null))
		{
			INode nodeWorldSpace_DEBUG = VoxelTerrain.GetNodeWorldSpace_DEBUG(InventoryManager.ParentPosition, DebugSize);
			if (nodeWorldSpace_DEBUG.GetSize() <= DebugSize)
			{
				nodeWorldSpace_DEBUG.DrawDebug();
			}
		}
	}
}
