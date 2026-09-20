using System;

namespace Assets.Scripts.Objects;

[Flags]
public enum AllowedRotations
{
	None = 0,
	Wall = 1,
	Ceiling = 2,
	Floor = 4,
	Vertical = 6,
	All = 7
}
