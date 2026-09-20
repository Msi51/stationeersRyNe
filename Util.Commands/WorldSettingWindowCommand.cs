using System;
using Assets.Scripts;
using Assets.Scripts.UI.ImGuiUi;

namespace Util.Commands;

internal class WorldSettingWindowCommand : CommandBase
{
	public override string HelpText => "Opens the (work-in-progress) ImGui window for editing live WorldSetting data. Not available on dedicated server builds.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("worldsettingwindow"))
		{
			return null;
		}
		ConsoleWindow.Hide();
		WorldSettingToolsImguiWindow.DrawOption();
		return "Displaying world setting window.";
	}
}
