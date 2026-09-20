namespace Assets.Scripts.Networking;

public class DismissHelperHintMessage : ProcessedMessage<DismissHelperHintMessage>
{
	public long ObjectiveStateId;

	public bool IsDismissed;

	public override void Process(long hostId)
	{
		Referencable.Find<WorldObjectiveState>(ObjectiveStateId)?.SetDismissed(IsDismissed);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out ObjectiveStateId);
		IsDismissed = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, ObjectiveStateId);
		writer.WriteBoolean(IsDismissed);
	}
}
