using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicMirror : LogicInputBase
{
	private long _currentDeviceIdOnJoin;

	private Device _currentDevice;

	private long _savedId;

	public Device CurrentDevice
	{
		get
		{
			return _currentDevice;
		}
		set
		{
			if (_currentDevice?.ReferenceId != value?.ReferenceId)
			{
				_currentDevice = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 1024;
				}
			}
		}
	}

	protected override bool IsOperable => CheckOperable();

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteInt64(CurrentDevice?.ReferenceId ?? 0);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			CurrentDevice = Thing.Find<Device>(reader.ReadInt64());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(CurrentDevice?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_savedId = reader.ReadInt64();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicMirrorSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicMirrorSaveData logicMirrorSaveData)
		{
			_savedId = logicMirrorSaveData.CurrentDeviceId;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicMirrorSaveData logicMirrorSaveData && (bool)CurrentDevice)
		{
			logicMirrorSaveData.CurrentDeviceId = CurrentDevice.ReferenceId;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		CurrentDevice = Thing.Find<Device>(_savedId);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (!CurrentDevice)
		{
			return false;
		}
		return CurrentDevice.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (!CurrentDevice || !Powered)
		{
			return 0.0;
		}
		return CurrentDevice.GetLogicValue(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (!CurrentDevice)
		{
			return false;
		}
		return CurrentDevice.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		if ((bool)CurrentDevice && Powered)
		{
			CurrentDevice.SetLogicValue(logicType, value);
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button1)
		{
			if (!CurrentDevice)
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return CurrentDevice.DisplayName;
		}
		return base.GetContextualName(interactable);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		foreach (Connection openEnd in OpenEnds)
		{
			if (hitCollider == openEnd.Collider && hitCollider != null)
			{
				result.Title = DisplayName;
				return result;
			}
		}
		result.Title = DisplayName;
		result.State = (CurrentDevice ? $"Mirroring <color=green>{CurrentDevice.DisplayName}</color>" : "No Device Set");
		return result;
	}

	private bool CheckOperable()
	{
		if (CurrentDevice == null || base.InputNetwork1 == null || !base.InputNetwork1.DataDeviceList.Contains(CurrentDevice))
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

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		if (interactable.Action == InteractableType.Button1)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			delayedActionInstance.SwitchTitleForTooltip = true;
			if (!(interaction.SourceSlot.Occupant is Screwdriver))
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			if (interactable.Action == InteractableType.Button1)
			{
				Device nextReadable = Logicable.GetNextReadable(this, CurrentDevice, base.InputNetwork1DevicesSorted, interaction.AltKey);
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
					CurrentDevice = nextReadable;
					CheckOperable();
				}
				return delayedActionInstance.Succeed();
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
