using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class SelectRocketMessage : ProcessedMessage<SelectRocketMessage>
{
	public long MotherboardRefId;

	public byte Index;

	public override void Process(long hostId)
	{
		RocketMotherboard rocketMotherboard = Referencable.Find<RocketMotherboard>(MotherboardRefId);
		if (!(rocketMotherboard == null))
		{
			rocketMotherboard.SelectedIndex = Index;
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out MotherboardRefId);
		Index = reader.ReadByte();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		Network.WritePackedId(writer, MotherboardRefId);
		writer.WriteByte(Index);
	}
}
