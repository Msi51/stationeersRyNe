using System;

namespace DLC;

[Flags]
public enum DLCType
{
	None = 0,
	Zrilian = 1,
	HemDroid = 2,
	HumanCharacter = 4,
	CountryOveralls = 8,
	BobbleHeadEva = 0x10,
	IcarusSuit = 0x20,
	BobbleHeadHard = 0x40,
	BobbleHeadMarine = 0x80,
	MetallicPaints = 0x100
}
