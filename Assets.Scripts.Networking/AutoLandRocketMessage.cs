using Objects.Rockets;

namespace Assets.Scripts.Networking;

public class AutoLandRocketMessage : ProcessedMessage<AutoLandRocketMessage>
{
	public long AvionicsId;

	public bool NewState;

	public override void Process(long hostId)
	{
		Referencable.Find<RocketAvionicsDevice>(AvionicsId).SetAutoLand(NewState);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out AvionicsId);
		NewState = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, AvionicsId);
		writer.WriteBoolean(NewState);
	}
}
