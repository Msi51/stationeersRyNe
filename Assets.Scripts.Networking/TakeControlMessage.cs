using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Networking;

public class TakeControlMessage : ProcessedMessage<TakeControlMessage>
{
	public long HumanId;

	public ulong ClientId;

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out HumanId);
		ClientId = reader.ReadUInt64();
		Human human = Thing.Find<Human>(HumanId);
		ConsoleWindow.Print($"Client {ClientId} took control of {human.DisplayName}");
		human.SetPhysicsOnControl();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, HumanId);
		writer.WriteUInt64(ClientId);
	}
}
