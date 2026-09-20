using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class CircuitHousing : LogicUnitBase, ICircuitHolder, IDensePoolable, IMemoryReadable, IMemory, IMemoryWritable
{
	public static readonly string[] SettingDisplayModeStrings = Enum.GetNames(typeof(SettingDisplayMode));

	private bool _hasPut;

	public const int RUN_COUNT = 128;

	[SerializeField]
	[ReadOnly]
	public ILogicable[] Devices = new ILogicable[6];

	private long[] _DeviceIDs = new long[6];

	private string[] _DeviceLabels = new string[6] { "", "", "", "", "", "" };

	private int _codeErrorState;

	private byte _processingUpdateFlags;

	public LogicLightComponent _MemoryLight;

	public override string[] ModeStrings => SettingDisplayModeStrings;

	public Slot _ProgrammableChipSlot => Slots[0];

	private ProgrammableChip ProgrammableChip => _ProgrammableChipSlot.Get<ProgrammableChip>();

	public ulong LastEditedBy { get; set; }

	protected override bool IsOperable
	{
		get
		{
			if (_codeErrorState == 0 && ProgrammableChip != null)
			{
				return !ProgrammableChip.CompilationError;
			}
			return false;
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.LogicIntegratedCircuitsCategory);
	}

	public void HasPut()
	{
		_hasPut = true;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteInt64(Devices[0]?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteInt64(Devices[1]?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteInt64(Devices[2]?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteInt64(Devices[3]?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteInt64(Devices[4]?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			writer.WriteInt64(Devices[5]?.ReferenceId ?? 0);
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			WriteProcessing(_processingUpdateFlags, writer);
			_processingUpdateFlags = 0;
		}
	}

	public override string GetStateText()
	{
		SettingDisplayMode settingDisplayMode = (SettingDisplayMode)Mode;
		if (settingDisplayMode != SettingDisplayMode.Number && settingDisplayMode == SettingDisplayMode.String)
		{
			return ProgrammableChip.UnpackAscii6(Setting, signed: true);
		}
		return Setting.ToStringExact();
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Devices[0] = Referencable.Find<ILogicable>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			Devices[1] = Referencable.Find<ILogicable>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			Devices[2] = Referencable.Find<ILogicable>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			Devices[3] = Referencable.Find<ILogicable>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			Devices[4] = Referencable.Find<ILogicable>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			Devices[5] = Referencable.Find<ILogicable>(reader.ReadInt64());
		}
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			ReadProcessing(reader.ReadByte(), reader);
		}
	}

	private void WriteProcessing(byte updateFlags, RocketBinaryWriter writer)
	{
		writer.WriteByte(updateFlags);
		if ((updateFlags & 0x40) != 0)
		{
			writer.WriteByte((byte)(_MemoryLight?.GetSyncState() ?? LogicMemoryState.Off));
		}
		if ((updateFlags & 1) != 0)
		{
			writer.WriteString(_DeviceLabels[0]);
		}
		if ((updateFlags & 2) != 0)
		{
			writer.WriteString(_DeviceLabels[1]);
		}
		if ((updateFlags & 4) != 0)
		{
			writer.WriteString(_DeviceLabels[2]);
		}
		if ((updateFlags & 8) != 0)
		{
			writer.WriteString(_DeviceLabels[3]);
		}
		if ((updateFlags & 0x10) != 0)
		{
			writer.WriteString(_DeviceLabels[4]);
		}
		if ((updateFlags & 0x20) != 0)
		{
			writer.WriteString(_DeviceLabels[5]);
		}
	}

	private void ReadProcessing(byte updateFlags, RocketBinaryReader reader)
	{
		if ((updateFlags & 0x40) != 0)
		{
			LogicMemoryState state = (LogicMemoryState)reader.ReadByte();
			_MemoryLight?.Flash(state);
		}
		if ((updateFlags & 1) != 0)
		{
			_DeviceLabels[0] = reader.ReadString();
		}
		if ((updateFlags & 2) != 0)
		{
			_DeviceLabels[1] = reader.ReadString();
		}
		if ((updateFlags & 4) != 0)
		{
			_DeviceLabels[2] = reader.ReadString();
		}
		if ((updateFlags & 8) != 0)
		{
			_DeviceLabels[3] = reader.ReadString();
		}
		if ((updateFlags & 0x10) != 0)
		{
			_DeviceLabels[4] = reader.ReadString();
		}
		if ((updateFlags & 0x20) != 0)
		{
			_DeviceLabels[5] = reader.ReadString();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		ILogicable[] devices = Devices;
		for (int i = 0; i < devices.Length; i++)
		{
			writer.WriteInt64(devices[i]?.ReferenceId ?? 0);
		}
		string[] deviceLabels = _DeviceLabels;
		foreach (string value in deviceLabels)
		{
			writer.WriteString(value);
		}
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		for (int i = 0; i < Devices.Length; i++)
		{
			_DeviceIDs[i] = reader.ReadInt64();
		}
		for (int j = 0; j < _DeviceLabels.Length; j++)
		{
			_DeviceLabels[j] = reader.ReadString();
		}
	}

	public async UniTask HaltAndCatchFire()
	{
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
			if (ThingTransform.GetCancellationTokenOnDestroy().IsCancellationRequested)
			{
				return;
			}
		}
		AtmosphericEventInstance.CloneGlobal(base.WorldGrid, MoleEnergy.Zero, spark: true);
		global::Explosion.Explode(50f, base.ThingTransformLocalPosition, 1f);
	}

	public List<ILogicable> GetBatchOutput()
	{
		if (base.InputNetwork1 == null)
		{
			return null;
		}
		return base.InputNetwork1DevicesSorted;
	}

	public List<LogicBinding> GetLogicBindings()
	{
		List<LogicBinding> list = new List<LogicBinding>();
		list.Add(new LogicBinding("HOUSING"));
		for (int i = 0; i < Devices.Length; i++)
		{
			list.Add(new LogicBinding(i, $"SCREW_{i}"));
		}
		return list;
	}

	public ILogicable GetLogicableFromIndex(int deviceIndex, int networkIndex = int.MinValue)
	{
		if (deviceIndex == int.MaxValue)
		{
			if (networkIndex != int.MinValue)
			{
				return GetNetwork(networkIndex);
			}
			return this;
		}
		if (base.InputNetwork1 != null && !base.InputNetwork1.DataDeviceList.Contains(Devices[deviceIndex] as Device))
		{
			return null;
		}
		if (networkIndex != int.MinValue)
		{
			return (Devices[deviceIndex] as IConnected)?.GetNetwork(networkIndex);
		}
		return Devices[deviceIndex];
	}

	public ILogicable GetLogicableFromId(int deviceId, int networkIndex = int.MinValue)
	{
		if (deviceId == 0L)
		{
			return null;
		}
		Device device = Referencable.Find<Device>(deviceId);
		if (base.InputNetwork1 != null && !base.InputNetwork1.DataDeviceList.Contains(device))
		{
			return null;
		}
		if (networkIndex != int.MinValue)
		{
			return ((IConnected)device)?.GetNetwork(networkIndex);
		}
		return device;
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
		}
	}

	public void Execute()
	{
		if (GameManager.RunSimulation && !IsCursor && OnOff && Powered && GameManager.GameState == GameState.Running && !WorldManager.IsGamePaused && GameManager.GameState != GameState.None && !(ProgrammableChip == null) && !ProgrammableChip.CompilationError)
		{
			ProgrammableChip.Execute(128);
		}
	}

	private string GetDeviceNameWithLabel(int deviceIndex)
	{
		string text = ((Devices[deviceIndex] != null) ? Devices[deviceIndex].DisplayName : ("<color=red>" + InterfaceStrings.LogicNoDevice + "</color>"));
		if ((bool)ProgrammableChip)
		{
			string text2 = _DeviceLabels[deviceIndex];
			if (!string.IsNullOrEmpty(text2))
			{
				text = $"<color=yellow>{text2}</color> " + text;
			}
		}
		return text;
	}

	public override string GetContextualName(Interactable interactable)
	{
		return interactable.Action switch
		{
			InteractableType.Button1 => GetDeviceNameWithLabel(0), 
			InteractableType.Button2 => GetDeviceNameWithLabel(1), 
			InteractableType.Button3 => GetDeviceNameWithLabel(2), 
			InteractableType.Button4 => GetDeviceNameWithLabel(3), 
			InteractableType.Button5 => GetDeviceNameWithLabel(4), 
			InteractableType.Button6 => GetDeviceNameWithLabel(5), 
			_ => base.GetContextualName(interactable), 
		};
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		DelayedActionInstance delayedActionInstance = base.InteractWith(interactable, interaction, doAction);
		ProgrammableChip?.AppendErrorsToActionInstance(delayedActionInstance);
		return interactable.Action switch
		{
			InteractableType.Button1 => Logicable._TryGetNextLogicDevice(interactable, interaction, ref Devices[0], base.InputNetwork1DevicesSorted, doAction, 0), 
			InteractableType.Button2 => Logicable._TryGetNextLogicDevice(interactable, interaction, ref Devices[1], base.InputNetwork1DevicesSorted, doAction, 1), 
			InteractableType.Button3 => Logicable._TryGetNextLogicDevice(interactable, interaction, ref Devices[2], base.InputNetwork1DevicesSorted, doAction, 2), 
			InteractableType.Button4 => Logicable._TryGetNextLogicDevice(interactable, interaction, ref Devices[3], base.InputNetwork1DevicesSorted, doAction, 3), 
			InteractableType.Button5 => Logicable._TryGetNextLogicDevice(interactable, interaction, ref Devices[4], base.InputNetwork1DevicesSorted, doAction, 4), 
			InteractableType.Button6 => Logicable._TryGetNextLogicDevice(interactable, interaction, ref Devices[5], base.InputNetwork1DevicesSorted, doAction, 5), 
			_ => delayedActionInstance, 
		};
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => true, 
			LogicType.LineNumber => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => true, 
			LogicType.LineNumber => true, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Setting:
			return Setting;
		case LogicType.LineNumber:
			if (!(ProgrammableChip != null))
			{
				return -1.0;
			}
			return ProgrammableChip.LineNumber;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.Setting:
			Setting = value;
			break;
		case LogicType.LineNumber:
			if (ProgrammableChip != null)
			{
				ProgrammableChip.LineNumber = (uint)value;
			}
			break;
		}
	}

	public override bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
	{
		if (slotId < 0 || slotId >= Slots.Count)
		{
			return false;
		}
		if (logicSlotType == LogicSlotType.LineNumber)
		{
			return true;
		}
		return base.CanLogicRead(logicSlotType, slotId);
	}

	public override double GetLogicValue(LogicSlotType logicSlotType, int slotId)
	{
		if (slotId < 0 || slotId >= Slots.Count)
		{
			return double.NaN;
		}
		if (logicSlotType == LogicSlotType.LineNumber && Slots[slotId].Occupant is ILogicable logicable)
		{
			return logicable.GetLogicValue(LogicType.LineNumber);
		}
		return base.GetLogicValue(logicSlotType, slotId);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new CircuitHousingSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (!(savedData is CircuitHousingSaveData circuitHousingSaveData))
		{
			return;
		}
		if (circuitHousingSaveData.DeviceIDs != null)
		{
			for (int i = 0; i < _DeviceIDs.Length && i < circuitHousingSaveData.DeviceIDs.Length; i++)
			{
				_DeviceIDs[i] = circuitHousingSaveData.DeviceIDs[i];
			}
		}
		if (circuitHousingSaveData.DeviceLabels != null)
		{
			for (int j = 0; j < _DeviceLabels.Length && j < circuitHousingSaveData.DeviceLabels.Length; j++)
			{
				_DeviceLabels[j] = circuitHousingSaveData.DeviceLabels[j];
			}
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is CircuitHousingSaveData circuitHousingSaveData)
		{
			circuitHousingSaveData.DeviceIDs = new long[Devices.Length];
			for (int i = 0; i < Devices.Length; i++)
			{
				circuitHousingSaveData.DeviceIDs[i] = ((Devices[i] == null) ? 0 : Devices[i].ReferenceId);
			}
			circuitHousingSaveData.DeviceLabels = new string[_DeviceLabels.Length];
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		for (int i = 0; i < _DeviceIDs.Length; i++)
		{
			Devices[i] = Thing.Find<Device>(_DeviceIDs[i]);
		}
		if ((bool)ProgrammableChip)
		{
			ClearError();
		}
		RefreshError();
	}

	public void ClearError()
	{
		RaiseError(0);
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		RefreshError();
	}

	public void RaiseError(int state)
	{
		_codeErrorState = state;
		RefreshError();
	}

	public void RefreshError()
	{
		if (GameManager.RunSimulation)
		{
			if (!IsOperable && Error == 0)
			{
				_MemoryLight?.Reset();
				OnServer.Interact(base.InteractError, 1);
			}
			else if (IsOperable && Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
	}

	public void SetSourceCode(string sourceCode)
	{
		if ((bool)ProgrammableChip)
		{
			ProgrammableChip.SetSourceCode(sourceCode, this);
			ProgrammableChip.SendUpdate();
			_MemoryLight?.Flash(LogicMemoryState.Write);
		}
	}

	public string GetSourceCode()
	{
		if ((object)ProgrammableChip != null)
		{
			_MemoryLight?.Flash(LogicMemoryState.Read);
			return ProgrammableChip.GetSourceCode();
		}
		return "";
	}

	public override void Update1000MS(float deltaTime)
	{
		base.Update1000MS(deltaTime);
		if (_hasPut && LastEditedBy != 0L)
		{
			Achievements.AchieveStackOverflow(LastEditedBy);
			_hasPut = false;
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		RefreshError();
		if (GameManager.GameState == GameState.Running)
		{
			ProgrammableChip.Reset();
			_MemoryLight?.Flash(LogicMemoryState.Write);
			if (NetworkManager.IsServer)
			{
				_processingUpdateFlags |= 63;
				base.NetworkUpdateFlags |= 512;
			}
			ClearError();
		}
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (previousChild is ProgrammableChip programmableChip)
		{
			programmableChip.Reset();
		}
		RefreshError();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable.Action != InteractableType.Error)
		{
			RefreshError();
		}
	}

	public bool IsValidIndex(int index)
	{
		if (index != int.MaxValue)
		{
			if (index >= 0)
			{
				return index < Devices.Length;
			}
			return false;
		}
		return true;
	}

	public void SetDeviceLabel(int index, string label)
	{
		if (index < 0 || index >= 6)
		{
			return;
		}
		string text = ((label.Length > 10) ? label.Substring(0, 10) : label);
		_DeviceLabels[index] = text;
		if (NetworkManager.IsServer)
		{
			switch (index)
			{
			case 0:
				_processingUpdateFlags |= 1;
				break;
			case 1:
				_processingUpdateFlags |= 2;
				break;
			case 2:
				_processingUpdateFlags |= 4;
				break;
			case 3:
				_processingUpdateFlags |= 8;
				break;
			case 4:
				_processingUpdateFlags |= 16;
				break;
			case 5:
				_processingUpdateFlags |= 32;
				break;
			}
			base.NetworkUpdateFlags |= 512;
		}
	}

	private void ResetDeviceLabels()
	{
		for (int i = 0; i < _DeviceLabels.Length; i++)
		{
			_DeviceLabels[i] = "";
		}
	}

	public double ReadMemory(int address)
	{
		if ((object)ProgrammableChip == null)
		{
			throw new NullReferenceException();
		}
		_MemoryLight?.Flash(LogicMemoryState.Read);
		return ProgrammableChip.ReadMemory(address);
	}

	public void WriteMemory(int address, double value)
	{
		if ((object)ProgrammableChip == null)
		{
			throw new NullReferenceException();
		}
		ProgrammableChip.WriteMemory(address, value);
		_MemoryLight?.Flash(LogicMemoryState.Write);
	}

	public void ClearMemory()
	{
		if ((object)ProgrammableChip == null)
		{
			throw new NullReferenceException();
		}
		ProgrammableChip.ClearMemory();
		_MemoryLight?.Flash(LogicMemoryState.Write);
	}

	public int GetStackSize()
	{
		return ProgrammableChip?.GetStackSize() ?? 0;
	}
}
