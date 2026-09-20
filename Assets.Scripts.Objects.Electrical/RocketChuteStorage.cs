using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Networks;
using Objects.Rockets;
using Objects.Rockets.Log;
using Objects.Rockets.Log.RocketEvents;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class RocketChuteStorage : DeviceImportExport, IRocketInternals, IRocketComponent, IRocketMassContributor
{
	[SerializeField]
	private int storageSlots = 50;

	public float RocketMass = 20f;

	private const int INDEX_OFFSET = 2;

	private int _currentIndex = 2;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public float MassContribution => RocketMass + (float)(CurrentIndex - 2);

	public override bool CanIceMelt => false;

	public int CurrentIndex
	{
		get
		{
			return _currentIndex;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients())
			{
				base.NetworkUpdateFlags |= 256;
			}
			_currentIndex = value;
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.CargoCategory);
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	protected override void OnImportClosingComplete()
	{
		base.OnImportClosingComplete();
		if (GameManager.RunSimulation)
		{
			TryProcessImport();
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		if (OnOff && Powered && IsOpen && IsNextExportReady)
		{
			CurrentIndex = Mathf.Max(CurrentIndex - 1, 2);
			DynamicThing occupant = Slots[CurrentIndex].Occupant;
			if (occupant != null)
			{
				OnServer.MoveToSlot(occupant, ExportSlot);
				occupant.ThingTransform.localScale = Vector3.one;
			}
		}
	}

	protected override void OnServerImportTick()
	{
		base.OnServerImportTick();
		if (CanBeginImport)
		{
			OnServer.Interact(base.InteractImport, 1);
		}
		if (base.IsImportClosed)
		{
			TryProcessImport();
		}
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
	}

	protected override void OnServerExportTick()
	{
		base.OnServerExportTick();
		if (CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
	}

	private void TryProcessImport()
	{
		if (ImportingThing == null)
		{
			return;
		}
		if (CurrentIndex >= Slots.Count)
		{
			CurrentIndex = Slots.Count;
			RocketLog.Append(new CargoStorageFullEvent(RocketNetwork?.Rocket, this)).Forget();
			return;
		}
		Slot slot = Slots[CurrentIndex];
		if (slot.Occupant == null)
		{
			OnServer.MoveToSlot(ImportingThing, slot);
			OnServer.Interact(base.InteractImport, 0);
		}
		CurrentIndex = Mathf.Min(CurrentIndex + 1, Slots.Count);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Quantity => true, 
			LogicType.Ratio => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Quantity => CurrentIndex - 2, 
			LogicType.Ratio => (float)(CurrentIndex - 2) / (float)storageSlots, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		RocketNetwork?.RecalculateStructureMass();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		RocketNetwork?.RecalculateStructureMass();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteByte((byte)CurrentIndex);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			CurrentIndex = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)CurrentIndex);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CurrentIndex = reader.ReadByte();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new RocketChuteStorageSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is RocketChuteStorageSaveData rocketChuteStorageSaveData)
		{
			CurrentIndex = rocketChuteStorageSaveData.CurrentIndex;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is RocketChuteStorageSaveData rocketChuteStorageSaveData)
		{
			rocketChuteStorageSaveData.CurrentIndex = CurrentIndex;
		}
	}
}
