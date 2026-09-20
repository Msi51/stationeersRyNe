using Assets.Scripts.Objects;

namespace Assets.Scripts.Networking;

public class MergeStackablesMessage : ProcessedMessage<MergeStackablesMessage>
{
	public long ParentItemId;

	public long ChildItemId;

	public override void Process(long hostId)
	{
		IMergeable parent = Thing.Find<IMergeable>(ParentItemId);
		IMergeable child = Thing.Find<IMergeable>(ChildItemId);
		OnServer.Merge(parent, child);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out ParentItemId);
		Network.ReadPackedId(reader, out ChildItemId);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, ParentItemId);
		Network.WritePackedId(writer, ChildItemId);
	}
}
