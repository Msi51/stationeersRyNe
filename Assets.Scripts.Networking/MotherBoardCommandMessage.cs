using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Networking;

public class MotherBoardCommandMessage : ProcessedMessage<MotherBoardCommandMessage>
{
	public MotherboardCommand Command;

	public bool BroadcastToAll;

	public override void Process(long hostId)
	{
		Command.Execute();
		if (BroadcastToAll)
		{
			Motherboard.NewCommands.Add(Command);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Command.Deserialize(reader);
		BroadcastToAll = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Command.Serialize(writer);
		writer.WriteBoolean(BroadcastToAll);
	}
}
