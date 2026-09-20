using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class ConstructionCreationMessage : ProcessedMessage<ConstructionCreationMessage>
{
	public int PrefabHash { get; set; }

	public Vector3 WorldPosition { get; set; }

	public Quaternion WorldRotation { get; set; }

	public Quaternion LocalRotation { get; set; }

	public ulong OwnerClientId { get; set; }

	public Grid3 LocalGrid { get; set; }

	public int CustomColorIndex { get; set; }

	public ConstructionCreationMessage()
	{
	}

	public ConstructionCreationMessage(CreateStructureInstance instance)
	{
		PrefabHash = instance.Prefab.PrefabHash;
		WorldPosition = instance.WorldPosition;
		WorldRotation = instance.WorldRotation;
		LocalRotation = instance.LocalRotation;
		OwnerClientId = instance.OwnerClientId;
		LocalGrid = instance.LocalGrid;
		CustomColorIndex = instance.Prefab.CustomColor.Index;
	}

	public override void Process(long hostId)
	{
		OnServer.Create<Structure>(PrefabHash, WorldPosition, WorldRotation).SetStructureData(LocalRotation, OwnerClientId, LocalGrid, CustomColorIndex);
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		PrefabHash = reader.ReadInt32();
		WorldPosition = reader.ReadVector3();
		WorldRotation = reader.ReadQuaternion();
		LocalRotation = reader.ReadQuaternion();
		OwnerClientId = reader.ReadUInt64();
		LocalGrid = reader.ReadGrid3();
		CustomColorIndex = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt32(PrefabHash);
		writer.WriteVector3(WorldPosition);
		writer.WriteQuaternion(WorldRotation);
		writer.WriteQuaternion(LocalRotation);
		writer.WriteUInt64(OwnerClientId);
		writer.WriteGrid3(LocalGrid);
		writer.WriteInt32(CustomColorIndex);
	}
}
