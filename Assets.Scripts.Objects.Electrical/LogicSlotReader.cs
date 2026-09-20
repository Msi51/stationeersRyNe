using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicSlotReader : LogicReaderDeviceBase
{
	private LogicSlotType _logicSlotType;

	private int _slotIndex = -1;

	private int _slotIndexSerialized;

	private LogicSlotType _index;

	private double _checkValue;

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
				_logicSlotType = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 2048;
				}
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
			if (base.CurrentDevice == null || SlotIndex < 0)
			{
				return null;
			}
			return base.CurrentDevice.GetSlot(SlotIndex);
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (base.CurrentDevice == null || SlotIndex < 0 || !base.CurrentDevice.CanLogicRead(LogicSlotType, SlotIndex) || base.InputNetwork1 == null || !base.InputNetwork1.DataDeviceList.Contains(base.CurrentDevice))
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
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			SlotIndex = reader.ReadInt16();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)LogicSlotType);
		writer.WriteInt16((short)SlotIndex);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		LogicSlotType = (LogicSlotType)reader.ReadByte();
		SlotIndex = reader.ReadInt16();
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
		if (base.CurrentDevice == null)
		{
			result.Extended = InterfaceStrings.LogicNoDevice.ToString("red");
			return result;
		}
		if (Slot == null)
		{
			result.Extended = Localization.GetInterface("LogicNoSlot");
			return result;
		}
		result.Extended = Slot.Parent.ToTooltip() + "." + Slot.ToTooltip() + "<color=yellow>." + EnumCollections.LogicSlotTypes.GetName(LogicSlotType) + "</color> = <color=yellow>" + Setting.ToStringExact() + "</color>";
		return result;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicSlotReaderSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicSlotReaderSaveData logicSlotReaderSaveData)
		{
			_savedId = logicSlotReaderSaveData.CurrentDeviceId;
			_index = (LogicSlotType)logicSlotReaderSaveData.InputIndex;
			_slotIndexSerialized = logicSlotReaderSaveData.SlotIndex;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicSlotReaderSaveData logicSlotReaderSaveData && (bool)base.CurrentDevice)
		{
			logicSlotReaderSaveData.CurrentDeviceId = base.CurrentDevice.ReferenceId;
			logicSlotReaderSaveData.InputIndex = (int)LogicSlotType;
			logicSlotReaderSaveData.SlotIndex = SlotIndex;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		LogicSlotType = _index;
		SlotIndex = _slotIndexSerialized;
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (OnOff && Powered && IsOperable)
		{
			_checkValue = base.CurrentDevice.GetLogicValue(LogicSlotType, SlotIndex);
			if (!RocketMath.Approximately(_checkValue, Setting))
			{
				Setting = _checkValue;
			}
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button1)
		{
			if (!base.CurrentDevice)
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return base.CurrentDevice.DisplayName;
		}
		if (interactable.Action == InteractableType.Button2)
		{
			if (Slot == null)
			{
				return "No Slots";
			}
			return Slot.DisplayName;
		}
		if (interactable.Action == InteractableType.Button3)
		{
			if (!base.CurrentDevice)
			{
				return InterfaceStrings.LogicNoSetting;
			}
			return EnumCollections.LogicSlotTypes.GetName(LogicSlotType);
		}
		return base.GetContextualName(interactable);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2 || interactable.Action == InteractableType.Button3)
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
			case InteractableType.Button1:
			{
				Device nextReadable = Logicable.GetNextReadable(this, base.CurrentDevice, base.InputNetwork1DevicesSorted, interaction.AltKey);
				if (!nextReadable)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoReadableDevices);
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, nextReadable.ToTooltip());
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				ScrewSound();
				if (GameManager.RunSimulation)
				{
					LogicSlotType = LogicSlotType.None;
					SlotIndex = -1;
					base.CurrentDevice = nextReadable;
					Setting = 0.0;
				}
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button2:
			{
				if (!base.CurrentDevice)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoDevice);
				}
				if (!base.CurrentDevice.IsLogicSlotReadable())
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoAvailableSlots);
				}
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				int nextSlot = Logicable.GetNextSlot(SlotIndex, base.CurrentDevice, interaction.AltKey);
				Slot slot = Logicable.GetSlot(nextSlot, base.CurrentDevice);
				if (nextSlot < 0 || slot == null)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoAdditionalSlotsFor, base.CurrentDevice.ToTooltip());
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSlotSettingToFor, EnumCollections.InteractableTypes.GetName(slot.Action), slot.ToTooltip(), slot.Parent.ToTooltip());
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
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button3:
			{
				if (!base.CurrentDevice)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoDevice);
				}
				if (!base.CurrentDevice.IsLogicSlotReadable())
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoSetting);
				}
				if (Slot == null)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoSlot);
				}
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				LogicSlotType nextReadableValue = Logicable.GetNextReadableValue(LogicSlotType, base.CurrentDevice, SlotIndex, interaction.AltKey);
				if ((int)nextReadableValue < 0)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoAdditionalSettingsFor, base.CurrentDevice.ToTooltip());
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingToForIn, EnumCollections.LogicSlotTypes.GetName(nextReadableValue), Slot.Parent.ToTooltip(), EnumCollections.InteractableTypes.GetName(Slot.Action), Slot.ToTooltip());
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
