using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Networking;

public class SetLogicReaderMessage : ProcessedMessage<SetLogicReaderMessage>
{
	public long LogicReaderId;

	public long DeviceId;

	public LogicType LogicType;

	public override void Process(long hostId)
	{
		LogicReader logicReader = Thing.Find<LogicReader>(LogicReaderId);
		Device currentDevice = Thing.Find<Device>(DeviceId);
		logicReader.CurrentDevice = currentDevice;
		logicReader.LogicType = LogicType;
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out LogicReaderId);
		Network.ReadPackedId(reader, out DeviceId);
		LogicType = (LogicType)reader.ReadUInt16();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, LogicReaderId);
		Network.WritePackedId(writer, DeviceId);
		writer.WriteUInt16((ushort)LogicType);
	}
}
