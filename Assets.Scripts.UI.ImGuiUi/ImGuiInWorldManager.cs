using ImGuiNET.Unity;
using UI.ImGuiUi;
using UI.ImGuiUi.Debug;
using UnityEngine;

namespace Assets.Scripts.UI.ImGuiUi;

public class ImGuiInWorldManager : MonoBehaviour
{
	public static bool ImguiInWorldTestCube;

	public static DearImGui _dearImGui;

	public static Camera Camera => _dearImGui.Camera;

	public void Awake()
	{
		_dearImGui = GetComponent<DearImGui>();
	}

	public static void DrawDebugActions()
	{
		ImGuiAtmosphericDebug.DrawAction();
		ImGuiStructureNetworkDebug.DrawAction();
		ImGuiDebugHelper.DrawDebug();
		ImguiTerrainDebug.DrawDebug();
		ImguiProxyDebug.DrawDebug();
		ImGuiProfiler.DrawDebug();
	}
}
