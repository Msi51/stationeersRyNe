using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using ImGuiNET;
using TerrainSystem.Lods;
using UI.ImGuiUi.ImGuiWindows;
using UnityEngine;

namespace UI.ImGuiUi;

public class ImGuiLodDebugWindow : UI.ImGuiUi.ImGuiWindows.ImGuiWindow
{
	public static ImGuiLodDebugWindow Window = new ImGuiLodDebugWindow();

	public ImGuiLodDebugWindow()
		: base("Lod Debug", new Vector2(400f, 600f))
	{
	}

	public override void OnOpen()
	{
	}

	public override void OnClose()
	{
	}

	public override void DrawContent()
	{
		if (ImGui.BeginTabBar("###TabBar", (ImGuiTabBarFlags)40))
		{
			if (ImGui.BeginTabItem("Mesh Cache###MeshCacheTab"))
			{
				DrawMeshCacheDebug();
				ImGui.EndTabItem();
			}
			if (ImGui.BeginTabItem("Lod Object Cache###LodObjectCache"))
			{
				DrawLodObjectCacheDebug();
				ImGui.EndTabItem();
			}
			ImGui.EndTabBar();
		}
	}

	private void DrawMeshCacheDebug()
	{
		for (int i = 0; i < 6; i++)
		{
			ImGui.TextColored(ImGuiColor.Float4.Green, "LEVEL ");
			ImGui.SameLine();
			ImGui.TextColored(ImGuiColor.Float4.Green, StringManager.Get(i));
			ImGui.Text("Initial: ");
			ImGui.SameLine();
			ImGui.Text(StringManager.Get(LodManager.MeshesPerLevel[i]));
			(int total, int active, int deactivated, int uninitialised) tuple = LodManager.Instance.LodMeshCacheCounts(i);
			ImGui.Text("Current: ");
			ImGui.SameLine();
			ImGui.Text(StringManager.Get(tuple.total));
			ImGui.Text("Active: ");
			ImGui.SameLine();
			ImGui.Text(StringManager.Get(tuple.active));
			ImGui.Text("Deactivated: ");
			ImGui.SameLine();
			ImGui.Text(StringManager.Get(tuple.deactivated));
			ImGui.Text("Uninitialised: ");
			ImGui.SameLine();
			ImGui.Text(StringManager.Get(tuple.uninitialised));
		}
	}

	private void DrawLodObjectCacheDebug()
	{
		for (int i = 0; i < 6; i++)
		{
			ImGui.TextColored(ImGuiColor.Float4.Green, "LEVEL ");
			ImGui.SameLine();
			ImGui.TextColored(ImGuiColor.Float4.Green, StringManager.Get(i));
			ImGui.Text("Initial: ");
			ImGui.SameLine();
			ImGui.Text(StringManager.Get(LodObjectCache.ObjectsPerLevel[i]));
			ImGui.Text("Current: ");
			ImGui.SameLine();
			ImGui.Text(StringManager.Get(LodObjectCache.LodObjectPools[i].GetTotalCount()));
		}
	}
}
