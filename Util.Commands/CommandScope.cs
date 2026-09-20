using System;

namespace Util.Commands;

[Flags]
public enum CommandScope
{
	None = 0,
	InGame = 1,
	HostOrSinglePlayer = 2,
	MultiplayerOnly = 4,
	SinglePlayerOnly = 8,
	CreativeOnly = 0x10
}
