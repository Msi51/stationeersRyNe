using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicWriterSwitch : LogicWriterBase
{
	private LogicUnitBase _lastInput;

	private Device _lastOutput;

	private double _lastSetting;

	[SerializeField]
	private GenericAssignableAnimComponent switchAnimComponent;

	private long _savedCurrentOutputId;

	private long _savedCurrentInputId;

	private long _savedLastInputId;

	private long _savedLastOutputId;

	public override LogicUnitBase Input1 => this;

	protected override bool IsOperable
	{
		get
		{
			if (base.CurrentOutput == null || !base.CurrentOutput.IsLogicWritable() || base.OutputNetwork1 == null || !base.OutputNetwork1.DataDeviceList.Contains(base.CurrentOutput))
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
		ThingSaveData savedData = new LogicWriterSwitchSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicWriterSwitchSaveData logicWriterSwitchSaveData)
		{
			_savedCurrentOutputId = logicWriterSwitchSaveData.CurrentOutputId;
			_savedCurrentInputId = logicWriterSwitchSaveData.CurrentInputId;
			base.LogicType = logicWriterSwitchSaveData.LogicType;
			_savedLastInputId = logicWriterSwitchSaveData.LastInputId;
			_savedLastOutputId = logicWriterSwitchSaveData.LastOutputId;
			_lastSetting = logicWriterSwitchSaveData.LastSetting;
		}
		if (savedData is LogicWriterSaveData logicWriterSaveData)
		{
			_savedCurrentOutputId = logicWriterSaveData.CurrentOutputId;
			_savedCurrentInputId = logicWriterSaveData.CurrentInputId;
			base.LogicType = logicWriterSaveData.LogicType;
			_savedLastInputId = logicWriterSaveData.LastInputId;
			_savedLastOutputId = logicWriterSaveData.LastOutputId;
			_lastSetting = logicWriterSaveData.LastSetting;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicWriterSwitchSaveData logicWriterSwitchSaveData && (bool)base.CurrentOutput)
		{
			logicWriterSwitchSaveData.CurrentOutputId = base.CurrentOutput.ReferenceId;
			logicWriterSwitchSaveData.LogicType = base.LogicType;
			if ((bool)_lastOutput)
			{
				logicWriterSwitchSaveData.LastOutputId = _lastOutput.ReferenceId;
			}
			logicWriterSwitchSaveData.LastSetting = _lastSetting;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(base.CurrentOutput?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_savedCurrentOutputId = reader.ReadInt64();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		base.CurrentOutput = Thing.Find<Device>(_savedCurrentOutputId);
		if (GameManager.RunSimulation)
		{
			_lastInput = Thing.Find<LogicUnitBase>(_savedLastInputId);
			_lastOutput = Thing.Find<Device>(_savedLastOutputId);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		switchAnimComponent.RefreshState(skipAnimation);
	}

	protected override void _WriteValue()
	{
		base.CurrentOutput.SetLogicValue(base.LogicType, Activate);
		Setting = Activate;
		_lastSetting = Activate;
		_lastInput = Input1;
		_lastOutput = base.CurrentOutput;
		base._WriteValue();
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (OnOff && Powered && IsOperable)
		{
			if (!_IsInputDirty && (_lastInput == null || _lastOutput == null || _lastInput != Input1 || _lastOutput != base.CurrentOutput || !RocketMath.Approximately(_lastSetting, Activate)))
			{
				_IsInputDirty = true;
			}
			if (_IsInputDirty)
			{
				_WriteValue();
				_IsInputDirty = false;
			}
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button1)
		{
			if (!base.CurrentOutput)
			{
				return interactable.DisplayName;
			}
			return base.CurrentOutput.DisplayName;
		}
		if (interactable.Action == InteractableType.Button2)
		{
			if (!base.CurrentOutput)
			{
				return interactable.DisplayName;
			}
			return base.LogicType.ToString();
		}
		if (interactable.Action == InteractableType.Activate)
		{
			if (Activate != 1)
			{
				return ActionStrings.On;
			}
			return ActionStrings.Off;
		}
		return base.GetContextualName(interactable);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		if (interactable.Action == InteractableType.Activate)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!base.AllowInteraction)
			{
				return delayedActionInstance.Fail(GameStrings.InteractionTemporarilyDisabledOnThisObject);
			}
			if (!IsAuthorized(interaction.SourceThing))
			{
				return delayedActionInstance.Fail(GameStrings.AccessCardUnableToInteract);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractActivate, (Activate != 1) ? 1 : 0);
			}
			return delayedActionInstance.Succeed();
		}
		if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2)
		{
			DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!(interaction.SourceSlot.Occupant is Screwdriver))
			{
				return delayedActionInstance2.Fail(GameStrings.RequiresScrewdriver);
			}
			switch (interactable.Action)
			{
			case InteractableType.Button1:
			{
				Device nextWritable = Logicable.GetNextWritable(this, base.CurrentOutput, base.OutputNetwork1DevicesSorted, interaction.AltKey);
				if (!nextWritable)
				{
					return delayedActionInstance2.Fail(GameStrings.LogicNoWritableDevices);
				}
				delayedActionInstance2.AppendStateMessage(GameStrings.GlobalChangeSettingTo, nextWritable.ToTooltip());
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance2.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				if (!doAction)
				{
					return delayedActionInstance2.Succeed();
				}
				ScrewSound();
				if (GameManager.RunSimulation)
				{
					base.LogicType = LogicType.None;
					_lastSetting = 0.0;
					base.CurrentOutput = nextWritable;
				}
				return delayedActionInstance2.Succeed();
			}
			case InteractableType.Button2:
			{
				if (!base.CurrentOutput)
				{
					return delayedActionInstance2.Fail(GameStrings.LogicNoDevice);
				}
				if (!base.CurrentOutput.IsLogicWritable())
				{
					return delayedActionInstance2.Fail(GameStrings.LogicNoSetting);
				}
				LogicType nextWritableValue = Logicable.GetNextWritableValue(base.LogicType, base.CurrentOutput, interaction.AltKey);
				if ((int)nextWritableValue < 0)
				{
					return delayedActionInstance2.Fail(GameStrings.LogicNoAdditionalSettingsFor, base.CurrentOutput.ToTooltip());
				}
				delayedActionInstance2.AppendStateMessage(GameStrings.GlobalChangeSettingToFor, nextWritableValue.ToString(), base.CurrentOutput.ToTooltip());
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance2.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				if (!doAction)
				{
					return delayedActionInstance2.Succeed();
				}
				ScrewSound();
				if (GameManager.RunSimulation)
				{
					_lastSetting = 0.0;
					base.LogicType = nextWritableValue;
				}
				return delayedActionInstance2.Succeed();
			}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
