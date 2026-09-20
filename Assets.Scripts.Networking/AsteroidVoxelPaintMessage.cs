using TerrainSystem;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class AsteroidVoxelPaintMessage : ProcessedMessage<AsteroidVoxelPaintMessage>
{
	public Vector3 VoxelWorldPosition;

	public VoxelNodeType Type;

	public byte Density;

	public long ToolId;

	public override void Process(long hostId)
	{
		OnServer.PlaceVoxelAtWorldPosition(ToolId, VoxelWorldPosition, Type);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		VoxelWorldPosition = reader.ReadVector3();
		Type = (VoxelNodeType)reader.ReadByte();
		Density = reader.ReadByte();
		Network.ReadPackedId(reader, out ToolId);
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteVector3(VoxelWorldPosition);
		writer.WriteByte((byte)Type);
		writer.WriteByte(Density);
		Network.WritePackedId(writer, ToolId);
	}
}
