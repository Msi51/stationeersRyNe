using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class CommsMotherBoardFilterMessageResult : ProcessedMessage<CommsMotherBoardFilterMessageResult>
{
	public long CommsMotherBoardId;

	public int NewFilterValue;

	public override void Process(long hostId)
	{
		Thing.Find<CommsMotherboard>(CommsMotherBoardId).FilterByEnumToInt = NewFilterValue;
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		CommsMotherBoardId = reader.ReadInt64();
		NewFilterValue = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(CommsMotherBoardId);
		writer.WriteInt32(NewFilterValue);
	}
}
