using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class CreateStructureMessage : ProcessedMessage<CreateStructureMessage>
{
	public long ConstructorId;

	public long OffhandOccupantReferenceId;

	public Grid3 LocalPosition;

	public Quaternion Rotation;

	public ulong CreatorSteamId;

	public int OptionIndex;

	public int PrefabHash;

	public bool AuthoringMode;

	public override void Process(long hostId)
	{
		Stackable stackable = Thing.Find<Stackable>(ConstructorId);
		if (stackable == null && AuthoringMode)
		{
			stackable = Prefab.Find(PrefabHash) as Stackable;
		}
		if (stackable == null)
		{
			ConsoleWindow.PrintError($"CreateStructureMessage: constructor #{ConstructorId} not found");
			return;
		}
		Constructor constructor = stackable as Constructor;
		MultiConstructor multiConstructor = stackable as MultiConstructor;
		if ((bool)constructor)
		{
			constructor.Construct(LocalPosition, Rotation, AuthoringMode, CreatorSteamId);
		}
		if ((bool)multiConstructor)
		{
			multiConstructor.Construct(LocalPosition, Rotation, OptionIndex, Thing.Find<Item>(OffhandOccupantReferenceId), AuthoringMode, CreatorSteamId);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ConstructorId = reader.ReadInt64();
		OffhandOccupantReferenceId = reader.ReadInt64();
		LocalPosition = reader.ReadGrid3();
		Rotation = reader.ReadQuaternion();
		CreatorSteamId = reader.ReadUInt64();
		OptionIndex = reader.ReadInt32();
		PrefabHash = reader.ReadInt32();
		AuthoringMode = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ConstructorId);
		writer.WriteInt64(OffhandOccupantReferenceId);
		writer.WriteGrid3(LocalPosition);
		writer.WriteQuaternion(Rotation);
		writer.WriteUInt64(CreatorSteamId);
		writer.WriteInt32(OptionIndex);
		writer.WriteInt32(PrefabHash);
		writer.WriteBoolean(AuthoringMode);
	}
}
