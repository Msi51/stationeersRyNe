using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Electrical;

public class LogicUnitProcessor : LogicUnitBase
{
	private LogicUnitBase _input1;

	private LogicUnitBase _input2;

	private LogicUnitBase _input3;

	private CableNetwork _inputNetwork2;

	private double _lastSetting;

	private List<ILogicable> _inputNetwork2DevicesSorted;

	private CableNetwork _inputNetwork3;

	private List<ILogicable> _inputNetwork3DevicesSorted;

	public int Input2Index = 1;

	public int Input3Index = 3;

	private long _savedId1;

	private long _savedId2;

	private long _savedId3;

	protected double _checkValue;

	public LogicUnitBase Input1
	{
		get
		{
			return _input1;
		}
		set
		{
			_input1 = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
		}
	}

	public LogicUnitBase Input2
	{
		get
		{
			return _input2;
		}
		set
		{
			_input2 = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
		}
	}

	public LogicUnitBase Input3
	{
		get
		{
			return _input3;
		}
		set
		{
			_input3 = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 32768;
			}
		}
	}

	public CableNetwork InputNetwork2
	{
		get
		{
			return _inputNetwork2;
		}
		set
		{
			if (_inputNetwork2 != value)
			{
				_inputNetwork2 = value;
				LogicNetworkChange();
			}
		}
	}

	public List<ILogicable> InputNetwork2DevicesSorted
	{
		get
		{
			if (_inputNetwork2DevicesSorted == null)
			{
				_inputNetwork2DevicesSorted = Logicable.RecalculateSortedDevicesList(_inputNetwork2);
			}
			return _inputNetwork2DevicesSorted;
		}
	}

	public CableNetwork InputNetwork3
	{
		get
		{
			return _inputNetwork3;
		}
		set
		{
			if (_inputNetwork3 != value)
			{
				_inputNetwork3 = value;
				LogicNetworkChange();
			}
		}
	}

	public List<ILogicable> InputNetwork3DevicesSorted
	{
		get
		{
			if (_inputNetwork3DevicesSorted == null)
			{
				_inputNetwork3DevicesSorted = Logicable.RecalculateSortedDevicesList(_inputNetwork3);
			}
			return _inputNetwork3DevicesSorted;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (Input1 == null || Input2 == null || base.InputNetwork1 == null || InputNetwork2 == null || !base.InputNetwork1.DataDeviceList.Contains(Input1) || !InputNetwork2.DataDeviceList.Contains(Input2))
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

	public event Event WriterOnSettingsChangeEvent;

	public override void OnSettingChanged()
	{
		base.OnSettingChanged();
		if (base.ShouldPlayLogicSound)
		{
			PlayPooledAudioSound(Defines.Sounds.LogicMath, LogicUnitBase.SoundOffset);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteInt64(Input1?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteInt64(Input2?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			writer.WriteInt64(Input3?.ReferenceId ?? 0);
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.LogicProcessorsCategory);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			Input1 = Thing.Find<LogicUnitBase>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			Input2 = Thing.Find<LogicUnitBase>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			Input3 = Thing.Find<LogicUnitBase>(reader.ReadInt64());
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt64(Input1?.ReferenceId ?? 0);
		writer.WriteInt64(Input2?.ReferenceId ?? 0);
		writer.WriteInt64(Input3?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_savedId1 = reader.ReadInt64();
		_savedId2 = reader.ReadInt64();
		_savedId3 = reader.ReadInt64();
	}

	protected override void CheckConnections()
	{
		base.CheckConnections();
		if (Input2Index >= 0 && Input2Index < OpenEnds.Count)
		{
			Cable cable = OpenEnds[Input2Index].GetCable();
			InputNetwork2 = (cable ? cable.CableNetwork : null);
		}
		if (Input3Index >= 0 && Input3Index < OpenEnds.Count)
		{
			Cable cable2 = OpenEnds[Input3Index].GetCable();
			InputNetwork3 = (cable2 ? cable2.CableNetwork : null);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicMathSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicMathSaveData logicMathSaveData)
		{
			_savedId1 = logicMathSaveData.Input1;
			_savedId2 = logicMathSaveData.Input2;
			_savedId3 = logicMathSaveData.Input3;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicMathSaveData logicMathSaveData)
		{
			if ((bool)Input1)
			{
				logicMathSaveData.Input1 = Input1.ReferenceId;
			}
			if ((bool)Input2)
			{
				logicMathSaveData.Input2 = Input2.ReferenceId;
			}
			if ((bool)Input3)
			{
				logicMathSaveData.Input3 = Input3.ReferenceId;
			}
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		Input1 = Thing.Find<LogicUnitBase>(_savedId1);
		Input2 = Thing.Find<LogicUnitBase>(_savedId2);
		Input3 = Thing.Find<LogicUnitBase>(_savedId3);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}

	public virtual void Operation()
	{
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (this.WriterOnSettingsChangeEvent != null)
		{
			this.WriterOnSettingsChangeEvent();
		}
		LogicNetworkChange();
		if (OnOff && Powered && IsOperable)
		{
			Operation();
		}
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
				LogicUnitBase nextReadable2 = Logicable.GetNextReadable(this, Input1, base.InputNetwork1DevicesSorted, interaction.AltKey);
				if (!nextReadable2)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoReadableDevices);
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, nextReadable2.ToTooltip());
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
					Input1 = nextReadable2;
					Setting = 0.0;
				}
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button2:
			{
				LogicUnitBase nextReadable = Logicable.GetNextReadable(this, Input2, InputNetwork2DevicesSorted, interaction.AltKey);
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
					Input2 = nextReadable;
					Setting = 0.0;
				}
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button3:
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, _NextOperatorType(interaction.AltKey));
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
					SwitchOperator(interaction.AltKey);
				}
				return delayedActionInstance.Succeed();
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public virtual string _NextOperatorType(bool isForward)
	{
		return "";
	}

	public virtual void SwitchOperator(bool isForward)
	{
	}

	public override void OnNetworkChange()
	{
		base.OnNetworkChange();
		_inputNetwork2DevicesSorted = null;
		_inputNetwork3DevicesSorted = null;
	}
}
