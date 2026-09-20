using Assets.Scripts.Voxel;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class AsteroidVoxelMessage : ProcessedMessage<AsteroidVoxelMessage>
{
	public Vector3 VoxelWorldPosition;

	public int Type;

	public byte Density;

	public bool Mined;

	public float ForcePickingEffectAmount;

	public override void Process(long hostId)
	{
		OnServer.SetVoxel(VoxelWorldPosition, Density, (MinableType)Type, Mined);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		VoxelWorldPosition = reader.ReadVector3();
		Type = reader.ReadInt32();
		Density = reader.ReadByte();
		Mined = reader.ReadBoolean();
		ForcePickingEffectAmount = reader.ReadSingle();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteVector3(VoxelWorldPosition);
		writer.WriteInt32(Type);
		writer.WriteByte(Density);
		writer.WriteBoolean(Mined);
		writer.WriteSingle(ForcePickingEffectAmount);
	}
}
