using Objects.Rockets;

namespace Assets.Scripts.Objects.Electrical;

public class RocketActionStackAddress : WritableStackAddress
{
	public RocketMode Mode { get; private set; }

	public byte Progress { get; private set; }

	public RocketActionStackAddress(StackAddress input)
		: base(input.StackIndex, input.DoubleValue)
	{
		(byte, byte, byte) tuple = LogicStack.UnpackByteX2(input.IntegerValue);
		Mode = (RocketMode)tuple.Item2;
		Progress = tuple.Item3;
	}
}
