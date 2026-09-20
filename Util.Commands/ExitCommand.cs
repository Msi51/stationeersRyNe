using System;
using Assets.Scripts;

namespace Util.Commands;

public class ExitCommand : CommandBase
{
	public override string HelpText => "Leaves the current game session and returns to the start menu.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("exit"))
		{
			return null;
		}
		GameManager.LeaveGame();
		return "Leaving game session.";
	}
}
