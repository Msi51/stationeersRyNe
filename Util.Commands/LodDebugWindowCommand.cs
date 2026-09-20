using System;
using Assets.Scripts;
using UI.ImGuiUi;
using UI.ImGuiUi.ImGuiWindows;

namespace Util.Commands;

internal class LodDebugWindowCommand : CommandBase
{
	public override string HelpText => "Toggles the LOD debug information window. Not available on dedicated server builds.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("loddebug"))
		{
			return null;
		}
		ConsoleWindow.Hide();
		if (ImGuiLodDebugWindow.Window.IsShowing)
		{
			ImGuiWindowManager.Close(ImGuiLodDebugWindow.Window);
			return "Lod debug window closed.";
		}
		ImGuiWindowManager.Open(ImGuiLodDebugWindow.Window);
		return "Lod debug window open.";
	}
}
