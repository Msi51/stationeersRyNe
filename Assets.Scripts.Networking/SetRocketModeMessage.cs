using Objects.Rockets;

namespace Assets.Scripts.Networking;

public class SetRocketModeMessage : ProcessedMessage<SetRocketModeMessage>
{
	public long AvionicsId;

	public RocketMode Mode;

	public override void Process(long hostId)
	{
		Referencable.Find<RocketAvionicsDevice>(AvionicsId).SetRocketMode(Mode);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out AvionicsId);
		Mode = (RocketMode)reader.ReadByte();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, AvionicsId);
		writer.WriteByte((byte)Mode);
	}
}
