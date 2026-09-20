using System;

namespace Assets.Scripts;

[Flags]
public enum ClientUpdateFlag : byte
{
	None = 0,
	Name = 1,
	DaysLived = 2
}
