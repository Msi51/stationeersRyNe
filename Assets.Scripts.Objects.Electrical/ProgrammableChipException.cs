using System;

namespace Assets.Scripts.Objects.Electrical;

public class ProgrammableChipException : Exception
{
	public enum ICExceptionType : byte
	{
		None,
		Unknown,
		IncorrectVariable,
		IncorrectVariableType,
		IncorrectLogicType,
		IncorrectLogicSlotType,
		IncorrectArgumentCount,
		DeviceNotSet,
		JumpTagDuplicate,
		UnrecognisedInstruction,
		IndexOutOfRange,
		OutOfRegisterBounds,
		OutOfDeviceBounds,
		IncorrectReagentMode,
		UnhandledReagentMode,
		IncorrectReagentDevice,
		ChipCatchingFire,
		StackOverFlow,
		StackUnderFlow,
		ExtraDefine,
		DeviceListNull,
		IncorrectReagentType,
		DeviceNotSlotWriteable,
		ShiftOverflow,
		ShiftUnderflow,
		DeviceNotFound,
		MemoryNotReadable,
		MemoryNotWriteable,
		InvalidStringLength,
		InvalidStringNull,
		InvalidStringNonAscii,
		InvalidPreprocessHash,
		InvalidProcessBinary,
		InvalidPreprocessHex,
		LogicTypeIsNone,
		PayloadOverflow,
		PayloadUnderflow,
		InvalidInteger,
		AliasNotFound
	}

	public readonly ushort LineNumber;

	public readonly ICExceptionType ExceptionType;

	public ProgrammableChipException(ICExceptionType exceptionType, ushort lineNumber)
	{
		ExceptionType = exceptionType;
		LineNumber = lineNumber;
	}

	public ProgrammableChipException(ICExceptionType exceptionType, int lineNumber)
	{
		ExceptionType = exceptionType;
		LineNumber = (ushort)lineNumber;
	}
}
