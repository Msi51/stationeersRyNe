using System.IO;
using Assets.Scripts;
using Assets.Scripts.Serialization;

namespace Util.Commands;

internal class SettingsPathCommand : CommandBase
{
	public override string HelpText => "Overrides the path to settings.xml. Launch command only; falls back to the default location if not provided.";

	public override string[] Arguments => new string[1] { "<full-directory-path>" };

	public override bool IsLaunchCmd => true;

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		FileInfo fileInfo = new FileInfo(args[0]);
		Settings.SettingData.Path = fileInfo.FullName;
		ConsoleWindow.PrintAction("Set custom settings path: " + fileInfo.FullName + ".");
		return null;
	}
}
