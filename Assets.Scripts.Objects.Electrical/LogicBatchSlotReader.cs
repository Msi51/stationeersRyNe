using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicBatchSlotReader : LogicReaderBase
{
	private int _prefabHash;

	public Device CurrentPrefab;

	private LogicBatchMethod _batchMethod;

	private LogicSlotType _logicSlotType;

	private int _slotIndex = -1;

	private int _counter;

	private double _checkValue;

	private Device _sampleDevice;

	public int CurrentPrefabHash
	{
		get
		{
			return _prefabHash;
		}
		set
		{
			_prefabHash = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 4096;
			}
			CurrentPrefab = Prefab.Find(_prefabHash) as Device;
		}
	}

	public LogicBatchMethod BatchMethod
	{
		get
		{
			return _batchMethod;
		}
		set
		{
			_batchMethod = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags = 8192;
			}
		}
	}

	public LogicSlotType LogicSlotType
	{
		get
		{
			return _logicSlotType;
		}
		set
		{
			if (_logicSlotType != value)
			{
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 2048;
				}
				_logicSlotType = value;
				LogicChanged();
			}
		}
	}

	public int SlotIndex
	{
		get
		{
			return _slotIndex;
		}
		set
		{
			if (_slotIndex != value)
			{
				_slotIndex = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 16384;
				}
				LogicChanged();
			}
		}
	}

	public Slot Slot
	{
		get
		{
			if (SampleDevice == null || SlotIndex < 0)
			{
				return null;
			}
			return SampleDevice.GetSlot(SlotIndex);
		}
	}

	private Device SampleDevice
	{
		get
		{
			if (CurrentPrefab == null)
			{
				return null;
			}
			if ((bool)_sampleDevice && _sampleDevice.PrefabHash == CurrentPrefabHash)
			{
				return _sampleDevice;
			}
			_sampleDevice = ((base.InputNetwork1 != null) ? base.InputNetwork1.DataDeviceList.Find((Device d) => d.PrefabHash == CurrentPrefabHash) : CurrentPrefab);
			return _sampleDevice;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (base.InputNetwork1 == null || CurrentPrefab == null || !SampleDevice || SlotIndex < 0 || !SampleDevice.CanLogicRead(LogicSlotType, SlotIndex) || base.OutputNetwork1 == null || base.InputNetwork1.DataDeviceList.FindIndex((Device d) => d.PrefabHash == CurrentPrefabHash) < 0)
			{
				if (Error == 0)
				{
					OnServer.Interact(base.InteractError, 1);
				}
				return false;
			}
			if (Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return true;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteByte((byte)LogicSlotType);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteByte((byte)BatchMethod);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteInt32(CurrentPrefabHash);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteInt16((short)SlotIndex);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			LogicSlotType = (LogicSlotType)reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			BatchMethod = (LogicBatchMethod)reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			CurrentPrefabHash = reader.ReadInt32();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			SlotIndex = reader.ReadInt16();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)LogicSlotType);
		writer.WriteByte((byte)BatchMethod);
		writer.WriteInt32(CurrentPrefabHash);
		writer.WriteInt16((short)SlotIndex);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		LogicSlotType = (LogicSlotType)reader.ReadByte();
		BatchMethod = (LogicBatchMethod)reader.ReadByte();
		CurrentPrefabHash = reader.ReadInt32();
		SlotIndex = reader.ReadInt16();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicBatchSlotReaderSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicBatchSlotReaderSaveData logicBatchSlotReaderSaveData)
		{
			CurrentPrefabHash = logicBatchSlotReaderSaveData.CurrentInputHash;
			LogicSlotType = (LogicSlotType)logicBatchSlotReaderSaveData.InputIndex;
			SlotIndex = logicBatchSlotReaderSaveData.SlotIndex;
			BatchMethod = logicBatchSlotReaderSaveData.BatchMethod;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicBatchSlotReaderSaveData logicBatchSlotReaderSaveData)
		{
			logicBatchSlotReaderSaveData.CurrentInputHash = CurrentPrefabHash;
			logicBatchSlotReaderSaveData.InputIndex = (int)LogicSlotType;
			logicBatchSlotReaderSaveData.SlotIndex = SlotIndex;
			logicBatchSlotReaderSaveData.BatchMethod = BatchMethod;
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (!OnOff || !Powered || !IsOperable)
		{
			return;
		}
		switch (BatchMethod)
		{
		case LogicBatchMethod.Count:
		{
			_checkValue = 0.0;
			int count4 = base.InputNetwork1.DataDeviceList.Count;
			while (count4-- > 0)
			{
				if (base.InputNetwork1.DataDeviceList[count4].PrefabHash == CurrentPrefabHash)
				{
					_counter++;
				}
			}
			_checkValue = _counter;
			break;
		}
		case LogicBatchMethod.Average:
		case LogicBatchMethod.Sum:
		{
			_checkValue = 0.0;
			_counter = 0;
			int count2 = base.InputNetwork1.DataDeviceList.Count;
			while (count2-- > 0)
			{
				Device device2 = base.InputNetwork1.DataDeviceList[count2];
				if (device2.PrefabHash == CurrentPrefabHash)
				{
					_checkValue += device2.GetLogicValue(LogicSlotType, SlotIndex);
					_counter++;
				}
			}
			if (BatchMethod == LogicBatchMethod.Average)
			{
				_checkValue /= _counter;
			}
			break;
		}
		case LogicBatchMethod.Minimum:
		{
			_checkValue = double.PositiveInfinity;
			int count3 = base.InputNetwork1.DataDeviceList.Count;
			while (count3-- > 0)
			{
				Device device3 = base.InputNetwork1.DataDeviceList[count3];
				if (device3.PrefabHash == CurrentPrefabHash)
				{
					double logicValue2 = device3.GetLogicValue(LogicSlotType, SlotIndex);
					if (!(logicValue2 >= _checkValue))
					{
						_checkValue = logicValue2;
					}
				}
			}
			if (_checkValue >= double.PositiveInfinity)
			{
				_checkValue = 0.0;
			}
			break;
		}
		case LogicBatchMethod.Maximum:
		{
			_checkValue = 0.0;
			int count = base.InputNetwork1.DataDeviceList.Count;
			while (count-- > 0)
			{
				Device device = base.InputNetwork1.DataDeviceList[count];
				if (device.PrefabHash == CurrentPrefabHash)
				{
					double logicValue = device.GetLogicValue(LogicSlotType, SlotIndex);
					if (!(logicValue <= _checkValue))
					{
						_checkValue = logicValue;
					}
				}
			}
			break;
		}
		}
		if (!RocketMath.Approximately(_checkValue, Setting))
		{
			Setting = _checkValue;
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button1)
		{
			if (!SampleDevice)
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return SampleDevice.SourcePrefab.DisplayName;
		}
		if (interactable.Action == InteractableType.Button3)
		{
			return EnumCollections.LogicSlotTypes.GetName(LogicSlotType);
		}
		if (interactable.Action == InteractableType.Button2)
		{
			if (!SampleDevice || Slot == null)
			{
				return "No Slots";
			}
			return Slot.DisplayName;
		}
		if (interactable.Action == InteractableType.Button4)
		{
			return EnumCollections.LogicBatchMethods.GetName(BatchMethod);
		}
		return base.GetContextualName(interactable);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		foreach (Connection openEnd in OpenEnds)
		{
			if (hitCollider == openEnd.Collider && hitCollider != null)
			{
				return result.Populate(openEnd);
			}
		}
		if (SampleDevice == null)
		{
			result.Extended = InterfaceStrings.LogicNoDevice.ToString("red");
			return result;
		}
		if (Slot == null)
		{
			result.Extended = Localization.GetInterface("LogicNoSlot").ToString("red");
			return result;
		}
		result.Extended = string.Format("{1}.{2}.<color=yellow>{3}</color>.{4} = <color=yellow>{0}</color>", Setting.ToStringExact(), Slot.Parent.SourcePrefab.ToTooltip(), Slot.ToTooltip(), EnumCollections.LogicSlotTypes.GetName(LogicSlotType), EnumCollections.LogicBatchMethods.GetName(BatchMethod));
		return result;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2 || interactable.Action == InteractableType.Button3 || interactable.Action == InteractableType.Button4)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!(interaction.SourceSlot.Occupant is Screwdriver))
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			switch (interactable.Action)
			{
			case InteractableType.Button4:
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				ScrewSound();
				if (GameManager.RunSimulation)
				{
					BatchMethod = BatchMethod.GetNext(interaction.AltKey, isSorted: true);
				}
				return delayedActionInstance.Succeed();
			case InteractableType.Button1:
			{
				Device nextReadableType = Logicable.GetNextReadableType(CurrentPrefabHash, CurrentPrefab, base.InputNetwork1DevicesSorted, interaction.AltKey);
				if (!nextReadableType)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoReadableTypes);
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingToAll, nextReadableType.ToTooltip());
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				ScrewSound();
				if (GameManager.RunSimulation)
				{
					LogicSlotType = LogicSlotType.None;
					Setting = 0.0;
					SlotIndex = -1;
					CurrentPrefabHash = nextReadableType.PrefabHash;
				}
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button2:
			{
				if (!SampleDevice)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoDevice);
				}
				if (!SampleDevice.HasAnySlots)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoAvailableSlots);
				}
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				int nextSlot = Logicable.GetNextSlot(SlotIndex, SampleDevice, interaction.AltKey);
				Slot slot2 = Logicable.GetSlot(nextSlot, SampleDevice);
				if (nextSlot < 0 || slot2 == null)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoAdditionalSlotsFor, SampleDevice.ToTooltip());
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSlotSettingToFor, EnumCollections.InteractableTypes.GetName(slot2.Action), slot2.ToTooltip(), slot2.Parent.SourcePrefab.ToTooltip());
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				ScrewSound();
				if (GameManager.RunSimulation)
				{
					SlotIndex = nextSlot;
					Setting = 0.0;
				}
				break;
			}
			case InteractableType.Button3:
			{
				if (!SampleDevice)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoDevice);
				}
				if (!SampleDevice.IsLogicSlotReadable())
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoSetting);
				}
				if (SampleDevice.GetSlot(SlotIndex) == null)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoSlot);
				}
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				LogicSlotType nextReadableValue = Logicable.GetNextReadableValue(LogicSlotType, SampleDevice, SlotIndex, interaction.AltKey);
				Slot slot = SampleDevice.GetSlot(SlotIndex);
				if ((int)nextReadableValue < 0 || slot == null)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoAdditionalSettingsFor, SampleDevice.ToTooltip());
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSlotSettingToFor, EnumCollections.LogicSlotTypes.GetName(nextReadableValue), slot.Parent.SourcePrefab.ToTooltip(), EnumCollections.InteractableTypes.GetName(slot.Action));
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				ScrewSound();
				if (GameManager.RunSimulation)
				{
					LogicSlotType = nextReadableValue;
					Setting = 0.0;
				}
				return delayedActionInstance.Succeed();
			}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
