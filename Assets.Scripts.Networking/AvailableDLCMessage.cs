using DLC;

namespace Assets.Scripts.Networking;

public class AvailableDLCMessage : ProcessedMessage<AvailableDLCMessage>
{
	public ushort DLCType;

	public override void Process(long hostId)
	{
		SharedDLCManager.AddSharedDLC(DLCType);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		DLCType = reader.ReadUInt16();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteUInt16(DLCType);
	}
}
