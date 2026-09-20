using Assets.Scripts;

namespace Util.Commands;

public class PauseCommand : CommandBase
{
	public override string HelpText => "Pauses or unpauses the game for everyone (including clients).";

	public override string[] Arguments => new string[1] { "<true | false>" };

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (args.Length < 1)
		{
			return "Invalid syntax";
		}
		if (!bool.TryParse(args[0], out var result))
		{
			ConsoleWindow.PrintError("invalid bool '" + args[0] + "', expected 'true' or 'false'", suppressStacktrace: true);
			return null;
		}
		if (!EnforceScope(result ? "pause" : "unpause"))
		{
			return null;
		}
		WorldManager.SetGamePause(result);
		if (!result)
		{
			return "Game unpaused.";
		}
		return "Game paused.";
	}
}
