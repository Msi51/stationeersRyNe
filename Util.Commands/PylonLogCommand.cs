using Objects.Electrical;

namespace Util.Commands;

public class PylonLogCommand : CommandBase
{
	public override string HelpText => "Toggles verbose logging of pylon-network merge/rebuild decisions. Dev tool.";

	public override string[] Arguments => new string[1] { "[on | off]" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (args.Length != 0)
		{
			switch (args[0].ToLowerInvariant())
			{
			case "on":
			case "1":
			case "true":
				PylonHelper.DebugLogEnabled = true;
				break;
			case "off":
			case "0":
			case "false":
				PylonHelper.DebugLogEnabled = false;
				break;
			default:
				PylonHelper.DebugLogEnabled = !PylonHelper.DebugLogEnabled;
				break;
			}
		}
		else
		{
			PylonHelper.DebugLogEnabled = !PylonHelper.DebugLogEnabled;
		}
		return "Pylon network debug logging: " + (PylonHelper.DebugLogEnabled ? "ON" : "OFF");
	}
}
