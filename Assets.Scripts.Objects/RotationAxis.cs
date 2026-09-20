using System;

namespace Assets.Scripts.Objects;

[Flags]
public enum RotationAxis
{
	None = 0,
	X = 1,
	Y = 2,
	Z = 4,
	XY = 3,
	ZY = 6,
	ZX = 5,
	All = 7
}
