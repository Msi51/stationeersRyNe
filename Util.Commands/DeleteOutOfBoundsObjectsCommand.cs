using System;
using Assets.Scripts;

namespace Util.Commands;

internal class DeleteOutOfBoundsObjectsCommand : CommandBase
{
	public override string HelpText => "Removes every object and atmosphere in the world that is outside the playable bounds. Host or singleplayer only.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("deleteoutofbounds"))
		{
			return null;
		}
		GameManager.DeleteOutOfBoundsObjects();
		return null;
	}
}
