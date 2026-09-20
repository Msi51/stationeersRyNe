using Assets.Scripts.Networking;
using UnityEngine;

namespace Assets.Scripts.Objects.Entities;

public class LastSpawnData
{
	public Vector3 Position;

	public Quaternion Rotation;

	public LastSpawnData()
	{
	}

	public LastSpawnData(Vector3 position, Quaternion rotation)
	{
		Position = position;
		Rotation = rotation;
	}

	public static LastSpawnData Copy(LastSpawnData other)
	{
		return new LastSpawnData(other.Position, other.Rotation);
	}

	public static void Write(RocketBinaryWriter writer, LastSpawnData data)
	{
		if (data != null)
		{
			writer.WriteVector3(data.Position);
			writer.WriteQuaternion(data.Rotation);
		}
	}

	public static LastSpawnData Read(RocketBinaryReader reader)
	{
		if (reader.ReadBoolean())
		{
			Vector3 position = reader.ReadVector3();
			Quaternion rotation = reader.ReadQuaternion();
			return new LastSpawnData(position, rotation);
		}
		return null;
	}
}
