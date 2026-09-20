namespace Assets.Scripts.Objects.Electrical;

public class FabricatorCreateStackAddress : WritableStackAddress
{
	public int PrefabHash { get; private set; }

	public byte Quantity { get; private set; }

	public int InvalidHandler { get; private set; }

	public FabricatorCreateStackAddress(StackAddress input, int invalidHandler)
		: base(input.StackIndex, input.DoubleValue)
	{
		(byte, byte, int) tuple = LogicStack.UnpackByteInt32(input.IntegerValue);
		InvalidHandler = invalidHandler;
		PrefabHash = tuple.Item3;
		Quantity = tuple.Item2;
	}

	public void Update()
	{
		Quantity = LogicStack.UnpackByteInt32(Value).Item2;
	}

	public void Decrement()
	{
		(byte, byte, int) tuple = LogicStack.UnpackByteInt32(Value);
		int item = tuple.Item3;
		byte b = (byte)(tuple.Item2 - 1);
		PrefabHash = item;
		Quantity = b;
		Value = LogicStack.PackByteInt32(Opcode, b, item);
	}
}
