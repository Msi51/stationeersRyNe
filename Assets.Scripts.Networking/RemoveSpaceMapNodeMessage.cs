using Assets.Scripts.Objects.Motherboards;
using Objects.Rockets;

namespace Assets.Scripts.Networking;

public class RemoveSpaceMapNodeMessage : ProcessedMessage<RemoveSpaceMapNodeMessage>
{
	public long NodeRefId;

	public long MotherboardRefId;

	public override void Process(long hostId)
	{
		RocketMotherboard rocketMotherboard = Referencable.Find<RocketMotherboard>(MotherboardRefId);
		if (!(rocketMotherboard == null))
		{
			SpaceMapNode spaceMapNode = Referencable.Find<SpaceMapNode>(NodeRefId);
			if (spaceMapNode != null)
			{
				rocketMotherboard.RemoveSpaceMapNode(spaceMapNode);
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out MotherboardRefId);
		Network.ReadPackedId(reader, out NodeRefId);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, MotherboardRefId);
		Network.WritePackedId(writer, NodeRefId);
	}
}
