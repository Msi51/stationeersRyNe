using System;
using Rooms;

namespace Util.Commands;

public class RegenerateRoomsCommand : CommandBase
{
	public override string HelpText => "Regenerates all rooms for the current world by re-running the room evaluator over every grid.";

	public override string[] Arguments => Array.Empty<string>();

	public override bool IsLaunchCmd => false;

	public override CommandScope Scope => CommandScope.InGame | CommandScope.HostOrSinglePlayer;

	public override string Execute(string[] args)
	{
		if (!EnforceScope("regeneraterooms"))
		{
			return null;
		}
		RoomRegenerator.RegenerateRooms();
		return "Rooms regenerated.";
	}
}
