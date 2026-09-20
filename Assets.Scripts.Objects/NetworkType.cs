using System;

namespace Assets.Scripts.Objects;

[Flags]
public enum NetworkType
{
	None = 0,
	Pipe = 1,
	Power = 2,
	Data = 4,
	Chute = 8,
	Elevator = 0x10,
	PipeLiquid = 0x20,
	LandingPad = 0x40,
	LaunchPad = 0x80,
	RoboticArmRail = 0x100,
	PowerAndData = 6,
	All = int.MaxValue
}
