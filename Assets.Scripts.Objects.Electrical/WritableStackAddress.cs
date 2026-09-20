namespace Assets.Scripts.Objects.Electrical;

public class WritableStackAddress
{
	public readonly int StackIndex;

	public readonly byte Opcode;

	public long Value;

	public WritableStackAddress(int stackIndex, double value)
	{
		StackIndex = stackIndex;
		Value = ProgrammableChip.DoubleToLong(value, signed: true);
		Opcode = (byte)(Value & 0xFF);
	}

	public void Write(LogicStack stack)
	{
		stack[StackIndex] = ProgrammableChip.LongToDouble(Value);
	}
}
