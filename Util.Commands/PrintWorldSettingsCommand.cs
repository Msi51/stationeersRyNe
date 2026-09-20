using System;
using Assets.Scripts;

namespace Util.Commands;

internal class PrintWorldSettingsCommand : CommandBase
{
	public override string HelpText => "Prints the currently loaded WorldSetting (terrain, atmosphere, traders, etc.) to the console.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (WorldSetting.Current == null)
		{
			ConsoleWindow.PrintError("No world setting is loaded.", suppressStacktrace: true);
			return null;
		}
		WorldSetting.Current.DebugPrint();
		return null;
	}
}
