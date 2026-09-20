using Assets.Scripts;

namespace Util.Commands;

public class RocketBinaryCommands : CommandBase
{
	public override string HelpText => "Toggles per-frame logging of FixedUpdate timing for delta-update size diagnostics.";

	public override bool Hidden => true;

	public override string[] Arguments => new string[1] { "togglelogphysics" };

	public override bool IsLaunchCmd => false;

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (args[0] == "togglelogphysics")
		{
			GameManager.LogFixedUpdateTime = !GameManager.LogFixedUpdateTime;
			return "FixedUpdate timing log " + (GameManager.LogFixedUpdateTime ? "enabled" : "disabled") + ".";
		}
		ConsoleWindow.PrintError("Unknown subcommand '" + args[0] + "'.", suppressStacktrace: true);
		return null;
	}
}
