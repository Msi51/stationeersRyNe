using System;

namespace Assets.Scripts.Objects;

[Flags]
public enum WallBlockMode
{
	None = 0,
	Top = 1,
	Left = 2,
	Right = 4,
	Front = 8,
	Bottom = 0x10,
	Back = 0x20,
	TopLeft = 3,
	TopRight = 5,
	BottomLeft = 0x12,
	BottomRight = 0x14,
	FrontBack = 0x28
}
