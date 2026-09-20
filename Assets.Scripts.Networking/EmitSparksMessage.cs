using UnityEngine;

namespace Assets.Scripts.Networking;

public class EmitSparksMessage : ProcessedMessage<EmitSparksMessage>
{
	public Vector3 Position;

	public int Quantity;

	public bool IsGravity;

	public override void Process(long hostId)
	{
		if (!GameManager.RunSimulation)
		{
			WorldManager.Spark(Position, Quantity, IsGravity);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		Position = reader.ReadVector3();
		Quantity = reader.ReadInt32();
		IsGravity = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteVector3(Position);
		writer.WriteInt32(Quantity);
		writer.WriteBoolean(IsGravity);
	}
}
