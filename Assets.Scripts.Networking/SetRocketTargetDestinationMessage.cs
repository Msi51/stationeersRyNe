using Objects.Rockets;

namespace Assets.Scripts.Networking;

public class SetRocketTargetDestinationMessage : ProcessedMessage<SetRocketTargetDestinationMessage>
{
	public long AvionicsId;

	public long SpaceMapNodeId;

	public override void Process(long hostId)
	{
		RocketAvionicsDevice rocketAvionicsDevice = Referencable.Find<RocketAvionicsDevice>(AvionicsId);
		SpaceMapNode targetDestination = Referencable.Find<SpaceMapNode>(SpaceMapNodeId);
		rocketAvionicsDevice.SetTargetDestination(targetDestination);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out AvionicsId);
		Network.ReadPackedId(reader, out SpaceMapNodeId);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, AvionicsId);
		Network.WritePackedId(writer, SpaceMapNodeId);
	}
}
