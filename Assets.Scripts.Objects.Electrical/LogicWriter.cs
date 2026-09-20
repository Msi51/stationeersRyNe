using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class LogicWriter : LogicWriterBase
{
	protected LogicUnitBase _lastInput;

	protected Device _lastOutput;

	protected double _lastSetting;

	private long _savedCurrentOutputId;

	private long _savedCurrentInputId;

	private long _savedLastInputId;

	private long _savedLastOutputId;

	protected override bool IsOperable
	{
		get
		{
			if (Input1 == null || base.InputNetwork1 == null || base.CurrentOutput == null || !base.CurrentOutput.IsLogicWritable() || base.OutputNetwork1 == null || !base.OutputNetwork1.DataDeviceList.Contains(base.CurrentOutput) || !base.InputNetwork1.DataDeviceList.Contains(Input1))
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
		ThingSaveData savedData = new LogicWriterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
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
		if (savedData is LogicWriterSaveData logicWriterSaveData && (bool)base.CurrentOutput)
		{
			logicWriterSaveData.CurrentOutputId = base.CurrentOutput.ReferenceId;
			if ((bool)Input1)
			{
				logicWriterSaveData.CurrentInputId = Input1.ReferenceId;
			}
			logicWriterSaveData.LogicType = base.LogicType;
			if ((bool)_lastInput)
			{
				logicWriterSaveData.LastInputId = _lastInput.ReferenceId;
			}
			if ((bool)_lastOutput)
			{
				logicWriterSaveData.LastOutputId = _lastOutput.ReferenceId;
			}
			logicWriterSaveData.LastSetting = _lastSetting;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		Network.WritePackedId(writer, base.CurrentOutput);
		Network.WritePackedId(writer, Input1);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Network.ReadPackedId(reader, out _savedCurrentOutputId);
		Network.ReadPackedId(reader, out _savedCurrentInputId);
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		base.CurrentOutput = Thing.Find<Device>(_savedCurrentOutputId);
		Input1 = Thing.Find<LogicUnitBase>(_savedCurrentInputId);
		if (GameManager.RunSimulation)
		{
			_lastInput = Thing.Find<LogicUnitBase>(_savedLastInputId);
			_lastOutput = Thing.Find<Device>(_savedLastOutputId);
		}
	}

	protected override void _WriteValue()
	{
		base.CurrentOutput.SetLogicValue(base.LogicType, Input1.Setting);
		Setting = Input1.Setting;
		_lastSetting = Input1.Setting;
		_lastInput = Input1;
		_lastOutput = base.CurrentOutput;
		base._WriteValue();
	}

	public override void OnSettingChanged()
	{
		base.OnSettingChanged();
		if (base.ShouldPlayLogicSound)
		{
			PlayPooledAudioSound(Defines.Sounds.LogicWrite, LogicUnitBase.SoundOffset);
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (OnOff && Powered && IsOperable)
		{
			if (!_IsInputDirty && (_lastOutput == null || _lastInput == null || _lastInput != Input1 || _lastOutput != base.CurrentOutput || !RocketMath.Approximately(_lastSetting, Input1.Setting)))
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
			return EnumCollections.LogicTypes.GetName(base.LogicType);
		}
		if (interactable.Action == InteractableType.Button3)
		{
			if (!Input1)
			{
				return interactable.DisplayName;
			}
			return Input1.DisplayName;
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
				Device nextWritable = Logicable.GetNextWritable(this, base.CurrentOutput, base.OutputNetwork1DevicesSorted, interaction.AltKey);
				if (!nextWritable)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoWritableDevices);
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, nextWritable.ToTooltip());
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
					base.LogicType = LogicType.None;
					_lastSetting = 0.0;
					base.CurrentOutput = nextWritable;
				}
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button2:
			{
				if (!base.CurrentOutput)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoDevice);
				}
				if (!base.CurrentOutput.IsLogicWritable())
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoSetting);
				}
				LogicType nextWritableValue = Logicable.GetNextWritableValue(base.LogicType, base.CurrentOutput, interaction.AltKey);
				if ((int)nextWritableValue < 0)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoAdditionalSettingsFor, base.CurrentOutput.ToTooltip());
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingToFor, EnumCollections.LogicTypes.GetName(nextWritableValue), base.CurrentOutput.ToTooltip());
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
					_lastSetting = 0.0;
					base.LogicType = nextWritableValue;
				}
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button3:
			{
				LogicUnitBase nextReadable = Logicable.GetNextReadable(this, Input1, base.InputNetwork1DevicesSorted, interaction.AltKey);
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
					_lastSetting = 0.0;
					Input1 = nextReadable;
				}
				return delayedActionInstance.Succeed();
			}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
