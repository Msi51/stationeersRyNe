using System;

namespace Assets.Scripts.Objects;

[Flags]
public enum SmallGridBlock
{
	None = 0,
	Pipes = 1,
	Cables = 2,
	Devices = 4,
	Covers = 8,
	Rails = 0x10,
	PipesAndCables = 3,
	DevicesAndPipes = 5,
	DevicesAndCables = 6,
	PipesCablesAndDevices = 7,
	PipesCablesDevicesAndCovers = 0xF,
	EverythingExceptPipes = -2147483647,
	EverythingExceptCables = -2147483646,
	Everything = int.MaxValue
}
