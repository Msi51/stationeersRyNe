using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicBatchReader : LogicReaderBase
{
	private int _prefabHash;

	public Device CurrentPrefab;

	private LogicType _logicType;

	private LogicBatchMethod _batchMethod;

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
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 4096;
				}
				CurrentPrefab = Prefab.Find<Device>(_prefabHash);
			}
		}
	}

	public LogicType LogicType
	{
		get
		{
			return _logicType;
		}
		set
		{
			if (value != _logicType)
			{
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 2048;
				}
				_logicType = value;
			}
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
			if (base.InputNetwork1 == null || CurrentPrefab == null || !SampleDevice || !SampleDevice.IsLogicReadable() || base.OutputNetwork1 == null || base.InputNetwork1.DataDeviceList.FindIndex((Device d) => d.PrefabHash == CurrentPrefabHash) < 0)
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
			writer.WriteUInt16((ushort)LogicType);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteByte((byte)BatchMethod);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteInt32(CurrentPrefabHash);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			LogicType = (LogicType)reader.ReadUInt16();
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			BatchMethod = (LogicBatchMethod)reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			CurrentPrefabHash = reader.ReadInt32();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteUInt16((ushort)LogicType);
		writer.WriteByte((byte)BatchMethod);
		writer.WriteInt32(CurrentPrefabHash);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		LogicType = (LogicType)reader.ReadUInt16();
		BatchMethod = (LogicBatchMethod)reader.ReadByte();
		CurrentPrefabHash = reader.ReadInt32();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new LogicBatchReaderSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LogicBatchReaderSaveData logicBatchReaderSaveData)
		{
			CurrentPrefabHash = logicBatchReaderSaveData.CurrentOutputHash;
			LogicType = (LogicType)logicBatchReaderSaveData.InputIndex;
			BatchMethod = logicBatchReaderSaveData.BatchMethod;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LogicBatchReaderSaveData logicBatchReaderSaveData)
		{
			logicBatchReaderSaveData.CurrentOutputHash = CurrentPrefabHash;
			logicBatchReaderSaveData.InputIndex = (int)LogicType;
			logicBatchReaderSaveData.BatchMethod = BatchMethod;
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (OnOff && Powered && IsOperable)
		{
			double num = Device.BatchRead(BatchMethod, LogicType, CurrentPrefabHash, base.InputNetwork1DevicesSorted);
			if (!RocketMath.Approximately(num, Setting))
			{
				Setting = num;
			}
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button3)
		{
			if (!SampleDevice)
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return SampleDevice.SourcePrefab.DisplayName;
		}
		if (interactable.Action == InteractableType.Button2)
		{
			if (!SampleDevice)
			{
				return InterfaceStrings.LogicNoSetting;
			}
			return LogicType.ToString();
		}
		if (interactable.Action == InteractableType.Button1)
		{
			return BatchMethod.ToString();
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
				result = result.Populate(openEnd);
			}
		}
		if (SampleDevice == null)
		{
			result.Title = DisplayName;
			result.Extended = InterfaceStrings.LogicNoDevice.ToString("red");
			return result;
		}
		result.Title = DisplayName;
		result.Extended = string.Format("{1}.<color=yellow>{2}</color>.{3} = <color=yellow>{0}</color>", Setting.ToStringExact(), SampleDevice.SourcePrefab.ToTooltip(), LogicType, BatchMethod);
		return result;
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
				LogicBatchMethod next = BatchMethod.GetNext(interaction.AltKey, isSorted: true);
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, next.ToString());
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				PlayPooledAudioSound(Defines.Sounds.ScrewdriverSound, Vector3.zero);
				if (GameManager.RunSimulation)
				{
					BatchMethod = BatchMethod.GetNext(interaction.AltKey, isSorted: true);
				}
				return delayedActionInstance.Succeed();
			}
			case InteractableType.Button3:
			{
				Device nextReadableType = Logicable.GetNextReadableType(CurrentPrefabHash, CurrentPrefab, base.InputNetwork1DevicesSorted, interaction.AltKey);
				if (!nextReadableType)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoReadableTypes);
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingToAll, nextReadableType.ToTooltip());
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				PlayPooledAudioSound(Defines.Sounds.ScrewdriverSound, Vector3.zero);
				if (GameManager.RunSimulation)
				{
					LogicType = LogicType.None;
					Setting = 0.0;
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
				if (!SampleDevice.IsLogicReadable())
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoSetting);
				}
				LogicType nextReadableValue = Logicable.GetNextReadableValue(LogicType, SampleDevice, interaction.AltKey);
				if ((int)nextReadableValue < 0)
				{
					return delayedActionInstance.Fail(GameStrings.LogicNoAdditionalSettingsFor, CurrentPrefab.ToTooltip());
				}
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingToFor, nextReadableValue.ToString(), CurrentPrefab.ToTooltip());
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				PlayPooledAudioSound(Defines.Sounds.ScrewdriverSound, Vector3.zero);
				if (GameManager.RunSimulation)
				{
					LogicType = nextReadableValue;
					Setting = 0.0;
				}
				return delayedActionInstance.Succeed();
			}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
