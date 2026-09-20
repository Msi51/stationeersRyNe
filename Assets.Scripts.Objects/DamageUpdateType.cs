using System;

namespace Assets.Scripts.Objects;

[Flags]
public enum DamageUpdateType : ushort
{
	All = ushort.MaxValue,
	None = 0,
	Burn = 2,
	Brute = 4,
	Oxygen = 8,
	Hydration = 0x10,
	Radiation = 0x20,
	Starvation = 0x40,
	Toxic = 0x80,
	Stun = 0x100,
	Decay = 0x200
}
