using System;

namespace Assets.Scripts.Atmospherics;

[Flags]
public enum LiquidParticleDirection : byte
{
	None = 0,
	Left = 1,
	Right = 2,
	Forward = 4,
	Back = 8
}
