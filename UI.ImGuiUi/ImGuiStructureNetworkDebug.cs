using Assets.Scripts.Networks;
using Networks;

namespace UI.ImGuiUi;

public static class ImGuiStructureNetworkDebug
{
	public static bool DebugChuteNetworks;

	public static bool DebugRocketNetworks;

	public static void DrawAction()
	{
		if (DebugChuteNetworks)
		{
			foreach (ChuteNetwork allChuteNetwork in ChuteNetwork.AllChuteNetworks)
			{
				allChuteNetwork.OnImguiDraw();
			}
		}
		if (!DebugRocketNetworks)
		{
			return;
		}
		foreach (RocketNetwork allRocketNetwork in RocketNetwork.AllRocketNetworks)
		{
			allRocketNetwork.OnImguiDraw();
		}
	}
}
