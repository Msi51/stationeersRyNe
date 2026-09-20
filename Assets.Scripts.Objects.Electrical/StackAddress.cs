namespace Assets.Scripts.Objects.Electrical;

public struct StackAddress(int stackIndex, double value)
{
	public readonly int StackIndex = stackIndex;

	public readonly byte Opcode = (byte)(IntegerValue & 0xFF);

	public readonly long IntegerValue = ProgrammableChip.DoubleToLong(value, signed: true);

	public readonly double DoubleValue = value;

	public readonly long Payload = IntegerValue >> 8;
}
