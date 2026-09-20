using System;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicTransmitter : LogicInputBase, ITransmissionReceiver, ITransmitable, ILogicable, IReferencable, IEvaluable
{
	private bool _addedToTransmitters;

	public static string[] LogicTransmitterModeStrings = Enum.GetNames(typeof(LogicTransmitterMode));

	private double _savedSetting;

	private long _savedId;

	private LogicType _index;

	private ITransmitable _currentDevice;

	public override double Setting
	{
		get
		{
			if (IsActiveTransmitter)
			{
				return base.Setting;
			}
			return CurrentDevice?.GetLogicValue(LogicType.Setting) ?? 0.0;
		}
		set
		{
			if (IsActiveTransmitter)
			{
				base.Setting = value;
			}
			else
			{
				CurrentDevice?.SetLogicValue(LogicType.Setting, value);
			}
		}
	}

	public override string[] ModeStrings => LogicTransmitterModeStrings;

	public bool IsActiveTransmitter => Mode == 1;

	public override int TotalSlots
	{
		get
		{
			if (IsActiveTransmitter)
			{
				return Slots.Count;
			}
			return CurrentDevice.TotalSlots;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (CurrentDevice == null && !IsActiveTransmitter)
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

	public ITransmitable CurrentDevice
	{
		get
		{
			return _currentDevice;
		}
		set
		{
			if (_currentDevice != value)
			{
				_currentDevice = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 1024;
				}
				LogicChanged();
			}
		}
	}

	private void SwitchMode(LogicTransmitterMode mode)
	{
		switch (mode)
		{
		case LogicTransmitterMode.Passive:
			if (_addedToTransmitters)
			{
				Transmitters.AllTransmitters.Remove(this);
				_addedToTransmitters = false;
			}
			break;
		case LogicTransmitterMode.Active:
			if (!_addedToTransmitters)
			{
				Transmitters.AllTransmitters.Add(this);
				_addedToTransmitters = true;
			}
			break;
		}
	}

	public void OnTransmitterCreated()
	{
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicTransmitterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicTransmitterSaveData logicTransmitterSaveData)
		{
			_savedId = logicTransmitterSaveData.CurrentConnectedId;
			_savedSetting = logicTransmitterSaveData.Setting;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicTransmitterSaveData logicTransmitterSaveData && CurrentDevice != null)
		{
			logicTransmitterSaveData.CurrentConnectedId = CurrentDevice.ReferenceId;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		CurrentDevice = Thing.Find<ITransmitable>(_savedId);
		Setting = _savedSetting;
	}

	public override Slot GetSlot(int slotIndex)
	{
		if (IsActiveTransmitter)
		{
			return base.GetSlot(slotIndex);
		}
		return CurrentDevice?.GetSlot(slotIndex);
	}

	public override int GetNextSlotId(int slotIndex, bool isForward)
	{
		if (IsActiveTransmitter)
		{
			return base.GetNextSlotId(slotIndex, isForward);
		}
		if (CurrentDevice == null)
		{
			return -1;
		}
		return CurrentDevice.GetNextSlotId(slotIndex, isForward);
	}

	public override bool IsLogicSlotReadable()
	{
		if (IsActiveTransmitter)
		{
			return base.IsLogicSlotReadable();
		}
		if (CurrentDevice == null)
		{
			return false;
		}
		return CurrentDevice.IsLogicSlotReadable();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (IsActiveTransmitter)
		{
			if (logicType == LogicType.Setting)
			{
				return true;
			}
			return base.CanLogicRead(logicType);
		}
		if (CurrentDevice == null)
		{
			return false;
		}
		if (!Powered)
		{
			return false;
		}
		return CurrentDevice.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (IsActiveTransmitter)
		{
			if (logicType == LogicType.Setting)
			{
				return Setting;
			}
			return base.GetLogicValue(logicType);
		}
		if (CurrentDevice == null)
		{
			return 0.0;
		}
		if (!Powered)
		{
			return 0.0;
		}
		return CurrentDevice.GetLogicValue(logicType);
	}

	public override bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
	{
		if (IsActiveTransmitter)
		{
			return base.CanLogicRead(logicSlotType, slotId);
		}
		if (CurrentDevice == null)
		{
			return false;
		}
		return CurrentDevice.CanLogicRead(logicSlotType, slotId);
	}

	public override double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		if (IsActiveTransmitter)
		{
			return base.GetLogicValue(logicSlotType, slotId);
		}
		if (CurrentDevice == null)
		{
			return 0.0;
		}
		if (!Powered)
		{
			return 0.0;
		}
		return CurrentDevice.GetLogicValue(logicSlotType, slotId);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (IsActiveTransmitter)
		{
			if (logicType == LogicType.Setting)
			{
				return true;
			}
			return base.CanLogicWrite(logicType);
		}
		if (CurrentDevice == null)
		{
			return false;
		}
		return CurrentDevice.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if (IsActiveTransmitter)
		{
			if (logicType == LogicType.Setting)
			{
				Setting = value;
			}
			base.SetLogicValue(logicType, value);
		}
		else if (CurrentDevice != null && Powered)
		{
			CurrentDevice.SetLogicValue(logicType, value);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action == InteractableType.Mode)
		{
			SwitchMode((LogicTransmitterMode)Mode);
		}
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
		if (IsActiveTransmitter)
		{
			result.Title = DisplayName;
			result.Extended = string.Format("Active as {0}.{2} = <color=yellow>{1}</color>", ToTooltip(), Setting, "Setting".ToString("yellow"));
			return result;
		}
		if (CurrentDevice == null)
		{
			result.Title = DisplayName;
			result.Extended = InterfaceStrings.LogicNoDevice.ToString("red");
			return result;
		}
		result.Title = DisplayName;
		result.Extended = ((CurrentDevice != null) ? $"Mirroring <color=green>{CurrentDevice.DisplayName}</color>" : "No Device Set");
		return result;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Network.WritePackedId(writer, CurrentDevice);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Network.ReadPackedId(reader, out var referenceId);
			CurrentDevice = Thing.Find<ITransmitable>(referenceId);
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		Network.WritePackedId(writer, CurrentDevice);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Network.ReadPackedId(reader, out _savedId);
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button1)
		{
			if (CurrentDevice == null)
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return CurrentDevice.DisplayName;
		}
		return base.GetContextualName(interactable);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		if (interactable.Action == InteractableType.Mode)
		{
			if (IsLocked)
			{
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DeviceLocked);
			}
			if (!doAction)
			{
				return DelayedActionInstance.Success(interactable.ContextualName);
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractMode, (base.InteractMode.State != 1) ? 1 : 0);
				CurrentDevice = null;
				Setting = 0.0;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		if (interactable.Action == InteractableType.Button1)
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
			if (Mode == 1)
			{
				return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.ThingModeDoesNotSupportLinking);
			}
			ITransmitable nextReadOrWritable = Logicable.GetNextReadOrWritable(this, CurrentDevice, Transmitters.AllTransmitters, interaction.AltKey);
			if (nextReadOrWritable == null)
			{
				return delayedActionInstance.Fail(GameStrings.LogicNoReadableDevices);
			}
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, nextReadOrWritable.ToTooltip());
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
				CurrentDevice = nextReadOrWritable;
				Setting = 0.0;
			}
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
