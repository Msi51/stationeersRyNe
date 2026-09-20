using Objects.Rockets.Scanning;

namespace Assets.Scripts.Networking;

public class SetTransferTargetMessage : ProcessedMessage<SetTransferTargetMessage>
{
	public long SourceId;

	public long TargetId;

	public override void Process(long hostId)
	{
		IRocketTransferActionProgressable rocketTransferActionProgressable = Referencable.Find<IRocketTransferActionProgressable>(SourceId);
		IRocketActionProgressableTarget target = Referencable.Find<IRocketActionProgressableTarget>(TargetId);
		rocketTransferActionProgressable.SetTarget(target);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out SourceId);
		Network.ReadPackedId(reader, out TargetId);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, SourceId);
		Network.WritePackedId(writer, TargetId);
	}
}
