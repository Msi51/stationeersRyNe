using System;

namespace Assets.Scripts.Atmospherics;

[Flags]
public enum LiquidFlowDirection : byte
{
	None = 0,
	LeftFlowIn = 1,
	RightFlowIn = 2,
	ForwardFlowIn = 4,
	BackFlowIn = 8,
	LeftFlowOut = 0x10,
	RightFlowOut = 0x20,
	ForwardFlowOut = 0x40,
	BackFlowOut = 0x80
}
