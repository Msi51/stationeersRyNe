using Objects.Rockets;

namespace Assets.Scripts.Networking;

public class SetReEntryProfileMessage : ProcessedMessage<SetReEntryProfileMessage>
{
	public long AvionicsId;

	public ReEntryProfile ReEntryProfile;

	public override void Process(long hostId)
	{
		Referencable.Find<RocketAvionicsDevice>(AvionicsId).SetReEntryProfile(ReEntryProfile);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out AvionicsId);
		ReEntryProfile = (ReEntryProfile)reader.ReadByte();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, AvionicsId);
		writer.WriteByte((byte)ReEntryProfile);
	}
}
