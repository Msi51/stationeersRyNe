namespace Assets.Scripts.Objects.Electrical;

public class StackPointerStackAddress : WritableStackAddress
{
	public ushort Index;

	public StackPointerStackAddress(int stackIndex, double value)
		: base(stackIndex, value)
	{
		Index = LogicStack.UnpackUInt16(ProgrammableChip.DoubleToLong(value, signed: true)).Item2;
	}

	public void Set(ushort value)
	{
		Index = value;
		Value = LogicStack.PackUInt16(Opcode, Index);
	}

	public static implicit operator uint(StackPointerStackAddress stackPointer)
	{
		return stackPointer.Index;
	}

	public static implicit operator int(StackPointerStackAddress stackPointer)
	{
		return stackPointer.Index;
	}
}
