using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Networking;

public class SetLogicValueMessage : ProcessedMessage<SetLogicValueMessage>
{
	public long DeviceReferenceId;

	public LogicType LogicType;

	public double LogicValue;

	public override void Process(long hostId)
	{
		Thing.Find<ILogicable>(DeviceReferenceId).SetLogicValue(LogicType, LogicValue);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out DeviceReferenceId);
		Network.ReadLogicValue(reader, out LogicType, out LogicValue);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, DeviceReferenceId);
		Network.WriteLogicValue(writer, LogicType, LogicValue);
	}
}
