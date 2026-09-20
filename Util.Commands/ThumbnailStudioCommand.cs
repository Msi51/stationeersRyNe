using System;
using Assets.Scripts;
using UI.ImGuiUi;

namespace Util.Commands;

internal class ThumbnailStudioCommand : CommandBase
{
	public override string HelpText => "Toggles the Thumbnail Studio window for viewing and authoring thing thumbnails (gallery, colour/build-state/rotation preview, save with colour variants, linking). Works from the main menu or in-game. Not available on dedicated server builds.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (ThumbnailStudioWindow.IsOpen)
		{
			ThumbnailStudioWindow.Close();
			return "Thumbnail studio closed.";
		}
		ConsoleWindow.Hide();
		ThumbnailStudioWindow.Open();
		return "Thumbnail studio open.";
	}
}
