using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class ReagentReader : LogicReaderDeviceBase
{
	private LogicReagentMode _logicReagentMode;

	private Reagent _logicReagent = Reagent.AllReagents.First();

	private int _logicReagentHash;

	private LogicReagentMode _logicReagentModeSaved;

	private double _checkValue;

	public LogicReagentMode LogicReagentMode
	{
		get
		{
			return _logicReagentMode;
		}
		set
		{
			if (_logicReagentMode != value)
			{
				_logicReagentMode = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 2048;
				}
				LogicChanged();
			}
		}
	}

	public Reagent LogicReagent
	{
		get
		{
			return _logicReagent;
		}
		set
		{
			if (!object.Equals(_logicReagent, value))
			{
				_logicReagent = value;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 4096;
				}
				LogicChanged();
			}
		}
	}

	public IRequireReagent CurrentDeviceReagentUser => (IRequireReagent)base.CurrentDevice;

	protected override bool IsOperable
	{
		get
		{
			if (base.CurrentDevice == null || base.CurrentDevice.ReadableReagentMixture == null || base.InputNetwork1 == null || !base.InputNetwork1.DataDeviceList.Contains(base.CurrentDevice))
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

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)LogicReagentMode);
		writer.WriteInt32(LogicReagent.GetHashCode());
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		LogicReagentMode = (LogicReagentMode)reader.ReadByte();
		int hashCode = reader.ReadInt32();
		LogicReagent = Reagent.AllReagents.Find((Reagent r) => r.GetHashCode() == hashCode);
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			LogicReagentMode = (LogicReagentMode)reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			int hashCode = reader.ReadInt32();
			LogicReagent = Reagent.AllReagents.Find((Reagent r) => r.GetHashCode() == hashCode);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteByte((byte)LogicReagentMode);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteInt32(LogicReagent.GetHashCode());
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
		if (base.CurrentDevice == null)
		{
			result.Title = DisplayName;
			result.Extended = InterfaceStrings.LogicNoDevice.ToString("red");
			return result;
		}
		result.Title = DisplayName;
		result.Extended = ((LogicReagent == null) ? "" : string.Format("{1}.{2}<color=yellow>.{3}</color> = <color=yellow>{0}</color>", Setting.ToStringPrefix(LogicReagent.Unit), base.CurrentDevice.ToTooltip(), LogicReagentMode, (LogicReagentMode != LogicReagentMode.TotalContents) ? LogicReagent.DisplayName : GameStrings.OperatorAll.DisplayString));
		return result;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicReagentReaderSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicReagentReaderSaveData logicReagentReaderSaveData)
		{
			_savedId = logicReagentReaderSaveData.CurrentDeviceId;
			_logicReagentModeSaved = (LogicReagentMode)logicReagentReaderSaveData.ModeIndex;
			_logicReagentHash = logicReagentReaderSaveData.ReagentHash;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicReagentReaderSaveData logicReagentReaderSaveData && (bool)base.CurrentDevice)
		{
			logicReagentReaderSaveData.CurrentDeviceId = base.CurrentDevice.ReferenceId;
			logicReagentReaderSaveData.ReagentHash = LogicReagent.GetHashCode();
			logicReagentReaderSaveData.ModeIndex = (int)LogicReagentMode;
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		LogicReagentMode = _logicReagentModeSaved;
		LogicReagent = Reagent.AllReagents.Find((Reagent r) => r.GetHashCode() == _logicReagentHash);
		if (LogicReagent == null)
		{
			LogicReagent = Reagent.AllReagents.First();
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (OnOff && Powered && IsOperable)
		{
			switch (LogicReagentMode)
			{
			case LogicReagentMode.Contents:
				_checkValue = base.CurrentDevice.ReadableReagentMixture.Get(LogicReagent);
				break;
			case LogicReagentMode.Required:
				_checkValue = CurrentDeviceReagentUser.RequiredReagents.Get(LogicReagent);
				break;
			case LogicReagentMode.Recipe:
				_checkValue = CurrentDeviceReagentUser.CurrentRecipe.Get(LogicReagent);
				break;
			case LogicReagentMode.TotalContents:
				_checkValue = base.CurrentDevice.ReadableReagentMixture.TotalReagents;
				break;
			}
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
		if (interactable.Action == InteractableType.Button3)
		{
			if (LogicReagent != null)
			{
				return LogicReagent.DisplayName;
			}
			return "No Reagent";
		}
		if (interactable.Action == InteractableType.Button2)
		{
			if (!base.CurrentDevice)
			{
				return InterfaceStrings.LogicNoSetting;
			}
			return LogicReagentMode.ToString();
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
					LogicReagentMode = LogicReagentMode.Contents;
					base.CurrentDevice = nextReadable;
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
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				if (LogicReagentMode == LogicReagentMode.TotalContents)
				{
					return delayedActionInstance.Fail();
				}
				Reagent nextReagent = Logicable.GetNextReagent(LogicReagent, interaction.AltKey);
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeReagentSettingTo, LogicReagentMode.ToString(), nextReagent?.DisplayName ?? GameStrings.OperatorAll.DisplayString, base.CurrentDevice.ToTooltip());
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				ScrewSound();
				if (GameManager.RunSimulation)
				{
					LogicReagent = nextReagent;
					Setting = 0.0;
				}
				break;
			}
			case InteractableType.Button2:
			{
				if (!base.CurrentDevice)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoDevice);
				}
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				LogicReagentMode logicReagentMode = ((!(base.CurrentDevice is IRequireReagent)) ? ((LogicReagentMode == LogicReagentMode.Contents) ? LogicReagentMode.TotalContents : LogicReagentMode.Contents) : LogicReagentMode.GetNext(interaction.AltKey, isSorted: true));
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingToFor, logicReagentMode.ToString(), base.CurrentDevice.ToTooltip());
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				ScrewSound();
				if (GameManager.RunSimulation)
				{
					LogicReagentMode = logicReagentMode;
					Setting = 0.0;
				}
				return delayedActionInstance.Succeed();
			}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
