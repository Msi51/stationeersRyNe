using Assets.Scripts;
using Assets.Scripts.Serialization;

namespace Util.Commands;

public class LegacyCpuCommand : CommandBase
{
	public override string HelpText => "Toggles Legacy CPU mode in user settings. Recommended for users with CPUs below the recommended spec. Requires a game restart to take effect.";

	public override string[] Arguments => new string[1] { "<enable | disable>" };

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (!CommandBase.Get(args, 0, "type", out string result))
		{
			return null;
		}
		result = result.ToLower();
		if (!(result == "enable"))
		{
			if (!(result == "disable"))
			{
				ConsoleWindow.PrintError("Unknown option '" + args[0] + "'. Expected 'enable' or 'disable'.", suppressStacktrace: true);
				return null;
			}
			Settings.CurrentData.LegacyCpu = false;
		}
		else
		{
			Settings.CurrentData.LegacyCpu = true;
		}
		Settings.SaveSettings();
		return "Legacy CPU mode is now " + (Settings.CurrentData.LegacyCpu ? "enabled" : "disabled") + ". Restart required to take effect.";
	}
}
