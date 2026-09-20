using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicReader : LogicReaderDeviceBase
{
	private LogicType _logicType;

	private double _checkValue;

	private LogicType _index;

	public LogicType LogicType
	{
		get
		{
			return _logicType;
		}
		set
		{
			if (_logicType != value)
			{
				_logicType = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 2048;
				}
				LogicChanged();
			}
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (base.CurrentDevice == null || !base.CurrentDevice.CanLogicRead(LogicType) || base.InputNetwork1 == null || !base.InputNetwork1.DataDeviceList.Contains(base.CurrentDevice))
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

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicReaderSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicReaderSaveData logicReaderSaveData)
		{
			_savedId = logicReaderSaveData.CurrentDeviceId;
			_index = (LogicType)logicReaderSaveData.InputIndex;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicReaderSaveData logicReaderSaveData && (bool)base.CurrentDevice)
		{
			logicReaderSaveData.CurrentDeviceId = base.CurrentDevice.ReferenceId;
			logicReaderSaveData.InputIndex = (int)LogicType;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		base.CurrentDevice = Thing.Find<Device>(_savedId);
		LogicType = _index;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		foreach (Connection openEnd in OpenEnds)
		{
			if (hitCollider == openEnd.Collider && hitCollider != null)
			{
				return result.Populate(openEnd);
			}
		}
		if (base.CurrentDevice == null)
		{
			result.Title = DisplayName;
			result.Extended = InterfaceStrings.LogicNoDevice.ToString("red");
			return result;
		}
		result.Title = DisplayName;
		result.Extended = string.Format("{1}.<color=yellow>{2}</color> = <color=yellow>{0}</color>", Setting.ToStringExact(), base.CurrentDevice.ToTooltip(), LogicType);
		return result;
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteUInt16((ushort)LogicType);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		LogicType = (LogicType)reader.ReadUInt16();
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			LogicType = (LogicType)reader.ReadUInt16();
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteUInt16((ushort)LogicType);
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		LogicChanged();
		if (OnOff && Powered && IsOperable)
		{
			Calculate();
		}
	}

	public virtual void Calculate()
	{
		_checkValue = base.CurrentDevice.GetLogicValue(LogicType);
		if (!RocketMath.Approximately(_checkValue, Setting))
		{
			Setting = _checkValue;
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		return interactable.Action switch
		{
			InteractableType.Button1 => base.CurrentDevice ? base.CurrentDevice.DisplayName : InterfaceStrings.LogicNoDevice, 
			InteractableType.Button2 => base.CurrentDevice ? EnumCollections.LogicTypes.GetName(LogicType) : InterfaceStrings.LogicNoSetting, 
			_ => base.GetContextualName(interactable), 
		};
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		InteractableType action = interactable.Action;
		if (action != InteractableType.Button1 && action != InteractableType.Button2)
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
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
				LogicType = LogicType.None;
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
			if (!base.CurrentDevice.IsLogicReadable())
			{
				return delayedActionInstance.Fail(GameStrings.LogicNoSetting);
			}
			if (!KeyManager.GetButton(KeyMap.QuantityModifier))
			{
				delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
			}
			LogicType nextReadableValue = Logicable.GetNextReadableValue(LogicType, base.CurrentDevice, interaction.AltKey);
			if ((int)nextReadableValue < 0)
			{
				return delayedActionInstance.Fail(GameStrings.LogicNoAdditionalSettingsFor, base.CurrentDevice.ToTooltip());
			}
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingToFor, nextReadableValue.ToString(), base.CurrentDevice.ToTooltip());
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
				LogicType = nextReadableValue;
				Setting = 0.0;
			}
			return delayedActionInstance.Succeed();
		}
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}
}
