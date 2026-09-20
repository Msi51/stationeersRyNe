using System;

namespace Objects.Rockets.Mining;

[Flags]
public enum MineableDepositType
{
	None = 0,
	Ore = 1,
	ReagentMix = 2,
	Ice = 4,
	Junk = 8,
	Gas = 0x10
}
