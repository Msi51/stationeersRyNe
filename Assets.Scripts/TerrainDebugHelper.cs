using ImGuiNET;
using TerrainSystem;
using TerrainSystem.Lods;
using UnityEngine;

namespace Assets.Scripts;

public class TerrainDebugHelper
{
	public static bool DebugTerrainThreads;

	public static void DrawDebug()
	{
		if (!DebugTerrainThreads)
		{
			return;
		}
		if (ImGui.Begin("TerrainThreadDebug", (ImGuiWindowFlags)799693))
		{
			ImGui.PushID("TerrainThreadDebug");
			ImGui.SetWindowPos(new Vector2(ImGui.GetIO().DisplaySize.x - 50f - ImGui.GetWindowWidth(), 50f), ImGuiCond.Always);
			int index = 0;
			LodMeshWorker[] workers = WorkerCollections.LodMeshWorkers.Workers;
			for (int i = 0; i < workers.Length; i++)
			{
				workers[i].DrawInList(ref index);
			}
			ImGui.PopID();
		}
		ImGui.End();
	}
}
