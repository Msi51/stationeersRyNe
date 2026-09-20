using System;

namespace Assets.Scripts.Objects.Motherboards;

[Flags]
public enum LinkedControlMode
{
	None = 0,
	Locked = 1,
	Linked = 2,
	Active = 4
}
