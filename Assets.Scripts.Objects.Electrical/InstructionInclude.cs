using System;

namespace Assets.Scripts.Objects.Electrical;

[Flags]
public enum InstructionInclude
{
	None = 0,
	All = 0xFFFFFFF,
	RegisterIndex = 1,
	Alias = 2,
	Value = 4,
	JumpTag = 8,
	DeviceIndex = 0x10,
	Define = 0x20,
	Enum = 0x40,
	LogicType = 0x80,
	LogicSlotType = 0x100,
	LogicReagentMode = 0x200,
	LogicBatchMethod = 0x400,
	NetworkIndex = 0x800,
	MaskDefineValue = 0x64,
	MaskDeviceIndex = 0x812,
	MaskStoreIndex = 3,
	MaskDoubleValue = 0x6F,
	MaskIntValue = 0x6F,
	MaskJumpIndex = 0x6F
}
