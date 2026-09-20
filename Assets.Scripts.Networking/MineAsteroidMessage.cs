using UnityEngine;

namespace Assets.Scripts.Networking;

public class MineAsteroidMessage : ProcessedMessage<MineAsteroidMessage>
{
	public long ParentBrainId;

	public Vector3 WorldVoxelPosition;

	public double Amount;

	public long ToolId;

	public override void Process(long hostId)
	{
		OnServer.MineAsteroid(ParentBrainId, WorldVoxelPosition, (float)Amount, ToolId);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ParentBrainId = reader.ReadInt64();
		WorldVoxelPosition = reader.ReadVector3();
		Amount = reader.ReadDouble();
		ToolId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ParentBrainId);
		writer.WriteVector3(WorldVoxelPosition);
		writer.WriteDouble(Amount);
		writer.WriteInt64(ToolId);
	}
}
