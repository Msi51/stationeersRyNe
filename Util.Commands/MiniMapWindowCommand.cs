using System;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using UI.ImGuiUi;
using UI.ImGuiUi.ImGuiWindows;

namespace Util.Commands;

internal class MiniMapWindowCommand : CommandBase
{
	public override string HelpText => "Toggles the debug minimap window. Requires creative mode and a world with a generated minimap texture. Not available on dedicated server builds.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("minimap"))
		{
			return null;
		}
		ConsoleWindow.Hide();
		if (ImGuiMiniMapWindow.Window.IsShowing)
		{
			ImGuiWindowManager.Close(ImGuiMiniMapWindow.Window);
			return "Debug minimap window closed.";
		}
		if (GameManager.GameState == GameState.None || WorldSetting.Current?.Data?.TerrainSettings?.MiniMapTexture == null)
		{
			ConsoleWindow.PrintError("No valid mini map to display.", suppressStacktrace: true);
			return null;
		}
		if (!DifficultySetting.Current.Creative)
		{
			ConsoleWindow.PrintError("Debug mini map only available in creative mode.", suppressStacktrace: true);
			return null;
		}
		ImGuiWindowManager.Open(ImGuiMiniMapWindow.Window);
		return "Debug minimap window open.";
	}
}
