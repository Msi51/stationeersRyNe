using UnityEngine;

namespace Assets.Scripts.Networking;

public class WeldingEffect : ProcessedMessage<WeldingEffect>
{
	public long ItemId;

	public Vector3 Position;

	public bool IsEnabled;

	public override void Process(long hostId)
	{
		OnServer.WeldEffect(ItemId, Position, IsEnabled);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ItemId = reader.ReadInt64();
		Position = reader.ReadVector3();
		IsEnabled = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ItemId);
		writer.WriteVector3(Position);
		writer.WriteBoolean(IsEnabled);
	}
}
