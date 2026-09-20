using TerrainSystem;

namespace Util.Commands;

internal class RegionCommand : CommandBase
{
	private const string ARG_DEBUG = "debug";

	public override string HelpText => "Toggles terrain region debug drawing. Not supported in dedicated server batch mode.";

	public override string[] Arguments => new string[1] { "debug" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("region"))
		{
			return null;
		}
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (args[0] == "debug")
		{
			return HandleDebug(args);
		}
		return "Invalid syntax";
	}

	private string HandleDebug(string[] args)
	{
		RegionManager.DoDrawDebug = !RegionManager.DoDrawDebug;
		string text = (RegionManager.DoDrawDebug ? "enabled" : "disabled");
		return "Region debugging " + text + ".";
	}
}
