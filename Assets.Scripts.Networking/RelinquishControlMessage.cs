using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Networking;

public class RelinquishControlMessage : ProcessedMessage<RelinquishControlMessage>
{
	public ulong HumanId;

	public override void Process(long hostId)
	{
		ConsoleWindow.Print("processing relinquish control message");
		Brain.GetValidatedBrain(HumanId, out var playerBrain);
		if (playerBrain != null)
		{
			ConsoleWindow.Print($"Brain exists for id {HumanId}");
			OnServer.RelinquishBrain(playerBrain);
		}
		else
		{
			ConsoleWindow.Print($"Brain does not exist for id {HumanId}, cannot relinquish control.");
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		HumanId = reader.ReadUInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteUInt64(HumanId);
	}
}
