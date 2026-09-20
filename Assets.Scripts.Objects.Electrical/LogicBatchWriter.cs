using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class LogicBatchWriter : LogicWriterBase
{
	private int _prefabHash;

	private LogicUnitBase _lastInput;

	private double _lastSetting;

	public Device CurrentPrefab;

	private long _savedCurrentInputId;

	private long _savedLastInputId;

	private Device _sampleDevice;

	public int CurrentPrefabHash
	{
		get
		{
			return _prefabHash;
		}
		set
		{
			if (_prefabHash != value)
			{
				_prefabHash = value;
				CurrentPrefab = Prefab.Find(_prefabHash) as Device;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 4096;
				}
				if (this.OnPrefabSelectionChanged != null)
				{
					this.OnPrefabSelectionChanged();
				}
			}
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (Input1 == null || base.InputNetwork1 == null || CurrentPrefab == null || !SampleDevice || !SampleDevice.IsLogicWritable() || base.OutputNetwork1 == null || !base.InputNetwork1.DataDeviceList.Contains(Input1) || base.OutputNetwork1.DataDeviceList.FindIndex((Device d) => d.PrefabHash == CurrentPrefabHash) < 0)
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
			_sampleDevice = ((base.OutputNetwork1 != null) ? base.OutputNetwork1.DataDeviceList.Find((Device d) => d.PrefabHash == CurrentPrefabHash) : CurrentPrefab);
			return _sampleDevice;
		}
	}

	public event Event OnPrefabSelectionChanged;

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(CurrentPrefabHash);
		Network.WritePackedId(writer, Input1);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		CurrentPrefabHash = reader.ReadInt32();
		Network.ReadPackedId(reader, out _savedCurrentInputId);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			CurrentPrefabHash = reader.ReadInt32();
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteInt32(CurrentPrefabHash);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicBatchWriterSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicBatchWriterSaveData logicBatchWriterSaveData)
		{
			CurrentPrefabHash = logicBatchWriterSaveData.CurrentOutputHash;
			_savedCurrentInputId = logicBatchWriterSaveData.CurrentInputId;
			base.LogicType = logicBatchWriterSaveData.LogicType;
			_savedLastInputId = logicBatchWriterSaveData.LastInputId;
			_lastSetting = logicBatchWriterSaveData.LastSetting;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicBatchWriterSaveData logicBatchWriterSaveData)
		{
			logicBatchWriterSaveData.CurrentOutputHash = CurrentPrefabHash;
			if ((bool)Input1)
			{
				logicBatchWriterSaveData.CurrentInputId = Input1.ReferenceId;
			}
			logicBatchWriterSaveData.LogicType = base.LogicType;
			if ((bool)_lastInput)
			{
				logicBatchWriterSaveData.LastInputId = _lastInput.ReferenceId;
			}
			logicBatchWriterSaveData.LastSetting = _lastSetting;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		Input1 = Thing.Find<LogicUnitBase>(_savedCurrentInputId);
		_lastInput = Thing.Find<LogicUnitBase>(_savedLastInputId);
	}

	protected override void _WriteValue()
	{
		Setting = Input1.Setting;
		_lastSetting = Input1.Setting;
		_lastInput = Input1;
		int count = base.OutputNetwork1.DataDeviceList.Count;
		while (count-- > 0)
		{
			Device device = base.OutputNetwork1.DataDeviceList[count];
			if (device.PrefabHash == CurrentPrefabHash)
			{
				device.SetLogicValue(base.LogicType, _lastSetting);
			}
		}
		base._WriteValue();
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (OnOff && Powered && IsOperable)
		{
			if (!_IsInputDirty && (_lastInput == null || _lastInput != Input1 || !RocketMath.Approximately(_lastSetting, Input1.Setting)))
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
			if (!CurrentPrefab)
			{
				return interactable.DisplayName;
			}
			return CurrentPrefab.DisplayName;
		}
		if (interactable.Action == InteractableType.Button2)
		{
			if (!CurrentPrefab)
			{
				return interactable.DisplayName;
			}
			return base.LogicType.ToString();
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
				Device nextWritableType = Logicable.GetNextWritableType(CurrentPrefabHash, CurrentPrefab, base.OutputNetwork1DevicesSorted, interaction.AltKey);
				if (!nextWritableType)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoWritableDevices);
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingToAll, nextWritableType.ToTooltip());
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
					CurrentPrefabHash = nextWritableType.PrefabHash;
				}
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button2:
			{
				if (!SampleDevice)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoDevice);
				}
				if (!SampleDevice.IsLogicWritable())
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoSetting);
				}
				LogicType nextWritableValue = Logicable.GetNextWritableValue(base.LogicType, SampleDevice, interaction.AltKey);
				if ((int)nextWritableValue < 0)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoAdditionalSettingsFor, CurrentPrefab.ToTooltip());
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingToForAll, EnumCollections.LogicTypes.GetName(nextWritableValue), CurrentPrefab.ToTooltip());
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
