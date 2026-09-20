namespace Assets.Scripts.Objects.Electrical;

public class ExecutionLimitStackAddress : WritableStackAddress
{
	public uint Count => LogicStack.UnpackUInt32(Value).Item2;

	public ExecutionLimitStackAddress(int stackIndex, double value)
		: base(stackIndex, value)
	{
	}

	public bool False()
	{
		return Count == 0;
	}

	public void Decrement()
	{
		uint value = Count - 1;
		Value = LogicStack.PackUInt32(Opcode, value);
	}
}
