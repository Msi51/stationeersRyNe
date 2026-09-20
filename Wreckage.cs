using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

public class Wreckage : Stackable
{
	private int _wreckedParentPrefabHash;

	private Thing _wreckedParentPrefab;

	public override string DisplayName
	{
		get
		{
			if (!(_wreckedParentPrefab != null))
			{
				return base.DisplayName;
			}
			return base.DisplayName + " (" + _wreckedParentPrefab.DisplayName + ")";
		}
	}

	public int WreckedParentPrefabHash
	{
		get
		{
			return _wreckedParentPrefabHash;
		}
		set
		{
			_wreckedParentPrefabHash = value;
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 512;
			}
			_wreckedParentPrefab = Prefab.Find<Thing>(_wreckedParentPrefabHash);
		}
	}

	public override bool CanStack(IMergeable targetStack)
	{
		return targetStack is Wreckage;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is WreckageSaveData wreckageSaveData)
		{
			wreckageSaveData.WreckedParentPrefabHash = WreckedParentPrefabHash;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new WreckageSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is WreckageSaveData wreckageSaveData)
		{
			WreckedParentPrefabHash = wreckageSaveData.WreckedParentPrefabHash;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteInt32(WreckedParentPrefabHash);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			WreckedParentPrefabHash = reader.ReadInt32();
		}
	}
}
