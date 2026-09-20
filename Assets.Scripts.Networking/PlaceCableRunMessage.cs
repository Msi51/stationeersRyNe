using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Networking;

public class PlaceCableRunMessage : ProcessedMessage<PlaceCableRunMessage>
{
	public long CableGunId;

	public long CoilId;

	public Grid3 Origin;

	public Grid3 End;

	public ulong CreatorSteamId;

	public override void Process(long hostId)
	{
		CableGun cableGun = Thing.Find<CableGun>(CableGunId);
		if (cableGun == null)
		{
			ConsoleWindow.PrintError($"PlaceCableRunMessage: cable gun #{CableGunId} not found");
			return;
		}
		MultiConstructor multiConstructor = cableGun.CableSlot?.Occupant as MultiConstructor;
		if (multiConstructor == null || multiConstructor.ReferenceId != CoilId)
		{
			ConsoleWindow.PrintError($"PlaceCableRunMessage: coil #{CoilId} not loaded in cable gun #{CableGunId}");
		}
		else
		{
			cableGun.PlaceCableRun(multiConstructor, Origin, End, CreatorSteamId);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		CableGunId = reader.ReadInt64();
		CoilId = reader.ReadInt64();
		Origin = reader.ReadGrid3();
		End = reader.ReadGrid3();
		CreatorSteamId = reader.ReadUInt64();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(CableGunId);
		writer.WriteInt64(CoilId);
		writer.WriteGrid3(Origin);
		writer.WriteGrid3(End);
		writer.WriteUInt64(CreatorSteamId);
	}
}
