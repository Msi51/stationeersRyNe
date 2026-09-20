using UnityEngine;

namespace Assets.Scripts.Networking;

public class ThingSpawnMessage : ProcessedMessage<ThingSpawnMessage>
{
	public int PrefabHash;

	public Vector3 Position;

	public Quaternion Rotation;

	public long ConnectionId;

	public bool IsSpawned;

	public long ReferenceId;

	public override int GetHashCode()
	{
		return PrefabHash.GetHashCode() ^ Position.GetHashCode() ^ Rotation.GetHashCode();
	}

	public override void Process(long hostId)
	{
		base.Process(hostId);
		if (IsSpawned)
		{
			NetworkSpawner.FindSpawnedItem(this);
		}
		else
		{
			NetworkSpawner.SpawnOnServer(this);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		PrefabHash = reader.ReadInt32();
		Position = reader.ReadVector3();
		Rotation = reader.ReadQuaternion();
		ConnectionId = reader.ReadInt64();
		IsSpawned = reader.ReadBoolean();
		ReferenceId = reader.ReadInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt32(PrefabHash);
		writer.WriteVector3(Position);
		writer.WriteQuaternion(Rotation);
		writer.WriteInt64(ConnectionId);
		writer.WriteBoolean(IsSpawned);
		writer.WriteInt64(ReferenceId);
	}
}
