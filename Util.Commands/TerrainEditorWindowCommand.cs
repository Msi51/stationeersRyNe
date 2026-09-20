using System;
using Assets.Scripts;
using UI.ImGuiUi;
using UI.ImGuiUi.ImGuiWindows;

namespace Util.Commands;

internal class TerrainEditorWindowCommand : CommandBase
{
	public override string HelpText => "Toggles the heightmap import / terrain utility window. Not available on dedicated server builds.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		ConsoleWindow.Hide();
		if (ImGuiTerrainUtilityWindow.Window.IsShowing)
		{
			ImGuiWindowManager.Close(ImGuiTerrainUtilityWindow.Window);
			return "Terrain utility window closed.";
		}
		ImGuiWindowManager.Open(ImGuiTerrainUtilityWindow.Window);
		return "Terrain utility window open.";
	}
}
