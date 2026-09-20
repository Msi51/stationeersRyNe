using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class AdvancedAirlockControl : AirlockControlBase
{
	private static readonly PressurekPa DefaultPressure = Chemistry.OneAtmosphere;

	private PressurekPa _pressureInternal = DefaultPressure;

	private PressurekPa _pressureExternal = DefaultPressure;

	public Text ButtonPressureInternalText;

	public Text ButtonPressureExternalText;

	private IPoweredVent _exteriorPoweredVent;

	private IPoweredVent _interiorPoweredVent;

	private PressurekPa _pressurizeToo = Chemistry.OneAtmosphere;

	private const float _defaultPressureMax = 50662.5f;

	private static readonly PressurekPa DefaultPressureMax = new PressurekPa(50662.5);

	public PressurekPa PressureInternal
	{
		get
		{
			return _pressureInternal;
		}
		set
		{
			_pressureInternal = value;
			RefreshScreen();
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public PressurekPa PressureExternal
	{
		get
		{
			return _pressureExternal;
		}
		set
		{
			_pressureExternal = value;
			RefreshScreen();
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 256;
			}
		}
	}

	public override bool CanToggle
	{
		get
		{
			AdvancedAirlockState airlockControlState = AirlockControlState;
			return airlockControlState == AdvancedAirlockState.PressurizedInternal || airlockControlState == AdvancedAirlockState.PressurizedExternal;
		}
	}

	public IPoweredVent ExteriorPoweredVent
	{
		get
		{
			if (!(base.MasterMotherboard is AdvancedAirlockControl advancedAirlockControl))
			{
				return _exteriorPoweredVent;
			}
			return advancedAirlockControl.ExteriorPoweredVent;
		}
		set
		{
			if (base.MasterMotherboard is AdvancedAirlockControl advancedAirlockControl)
			{
				advancedAirlockControl._exteriorPoweredVent = value;
			}
			else
			{
				_exteriorPoweredVent = value;
			}
		}
	}

	public IPoweredVent InteriorPoweredVent
	{
		get
		{
			if (!(base.MasterMotherboard is AdvancedAirlockControl advancedAirlockControl))
			{
				return _interiorPoweredVent;
			}
			return advancedAirlockControl.InteriorPoweredVent;
		}
		set
		{
			if (base.MasterMotherboard is AdvancedAirlockControl advancedAirlockControl)
			{
				advancedAirlockControl._interiorPoweredVent = value;
			}
			else
			{
				_interiorPoweredVent = value;
			}
		}
	}

	public AdvancedAirlockState AirlockControlState
	{
		get
		{
			return (AdvancedAirlockState)base.Flag;
		}
		set
		{
			base.Flag = (int)value;
			RefreshScreen();
			foreach (Motherboard slafe in Slaves)
			{
				slafe.RefreshScreen();
			}
			if (GameManager.RunSimulation && AirlockProcessing.Status != UniTaskStatus.Pending && GameManager.GameState == GameState.Running)
			{
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
				switch (AirlockControlState)
				{
				case AdvancedAirlockState.PressurizingInternal:
					AirlockProcessing = Pressurizing(AdvancedAirlockState.PressurizingInternal, AdvancedAirlockState.PressurizedInternal);
					break;
				case AdvancedAirlockState.DepressurizingInternal:
					AirlockProcessing = Depressurizing(AdvancedAirlockState.DepressurizingInternal, AdvancedAirlockState.PressurizingExternal);
					break;
				case AdvancedAirlockState.PressurizingExternal:
					AirlockProcessing = Pressurizing(AdvancedAirlockState.PressurizingExternal, AdvancedAirlockState.PressurizedExternal);
					break;
				case AdvancedAirlockState.DepressurizingExternal:
					AirlockProcessing = Depressurizing(AdvancedAirlockState.DepressurizingExternal, AdvancedAirlockState.PressurizingInternal);
					break;
				case AdvancedAirlockState.PressurizedInternal:
				case AdvancedAirlockState.PressurizedExternal:
					break;
				}
			}
		}
	}

	public override bool IsOperable
	{
		get
		{
			if ((bool)(base.MasterMotherboard as AirlockControl))
			{
				return ((AirlockControl)base.MasterMotherboard).IsOperable;
			}
			if ((bool)base.ExteriorAirlock && base.ExteriorAirlock.IsLocked && (bool)base.InteriorAirlock && base.InteriorAirlock.IsLocked && base.GasSensors.Count > 0 && ExteriorPoweredVent != null)
			{
				return InteriorPoweredVent != null;
			}
			return false;
		}
	}

	public override bool IsError => AirlockControlState == AdvancedAirlockState.Disabled;

	public override void FlashCircuit()
	{
		base.FlashCircuit();
		PressureExternal = DefaultPressure;
		PressureInternal = DefaultPressure;
		_interiorPoweredVent = null;
		_exteriorPoweredVent = null;
		_PoweredVents.Clear();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(PressureExternal.ToFloat());
			writer.WriteSingle(PressureInternal.ToFloat());
			writer.WriteInt32((int)AirlockControlState);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			PressureExternal = new PressurekPa(reader.ReadSingle());
			PressureInternal = new PressurekPa(reader.ReadSingle());
			AirlockControlState = (AdvancedAirlockState)reader.ReadInt32();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(PressureExternal.ToFloat());
		writer.WriteSingle(PressureInternal.ToFloat());
		writer.WriteInt32((int)AirlockControlState);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		PressureExternal = new PressurekPa(reader.ReadSingle());
		PressureInternal = new PressurekPa(reader.ReadSingle());
		AirlockControlState = (AdvancedAirlockState)reader.ReadInt32();
	}

	public override string GetStateString()
	{
		return AirlockControlState.GetName();
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is AdvAirlockControldSaveData advAirlockControldSaveData)
		{
			advAirlockControldSaveData.PressureInternal = PressureInternal.ToFloat();
			advAirlockControldSaveData.PressureExternal = PressureExternal.ToFloat();
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new AdvAirlockControldSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
		if (saveData is AdvAirlockControldSaveData advAirlockControldSaveData)
		{
			PressureInternal = new PressurekPa(advAirlockControldSaveData.PressureInternal);
			PressureExternal = new PressurekPa(advAirlockControldSaveData.PressureExternal);
		}
		RefreshScreen();
	}

	public override void MotherboardCommand(int command, Thing reference, int referenceInt, string text)
	{
		switch ((ButtonCommands)command)
		{
		case ButtonCommands.Special1:
			if (GameManager.RunSimulation)
			{
				PressureExternal = new PressurekPa(referenceInt);
			}
			break;
		case ButtonCommands.Special2:
			if (GameManager.RunSimulation)
			{
				PressureInternal = new PressurekPa(referenceInt);
			}
			break;
		}
		base.MotherboardCommand(command, reference, referenceInt, text);
	}

	private async UniTask Pressurizing(AdvancedAirlockState transitState, AdvancedAirlockState endState)
	{
		if (!GameManager.RunSimulation || !IsOperable || (bool)base.MasterMotherboard)
		{
			RefreshScreen();
			return;
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		await WaitDoorClose(cancelToken);
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		switch (transitState)
		{
		case AdvancedAirlockState.PressurizingInternal:
			OnServer.Interact(InteriorPoweredVent.InteractOnOff, 1);
			OnServer.Interact(InteriorPoweredVent.InteractMode, 0);
			OnServer.Interact(ExteriorPoweredVent.InteractOnOff, 0);
			_pressurizeToo = PressureInternal;
			InteriorPoweredVent.ExternalPressure = _pressurizeToo;
			InteriorPoweredVent.InternalPressure = PressurekPa.Zero;
			break;
		case AdvancedAirlockState.PressurizingExternal:
			OnServer.Interact(ExteriorPoweredVent.InteractOnOff, 1);
			OnServer.Interact(ExteriorPoweredVent.InteractMode, 0);
			OnServer.Interact(InteriorPoweredVent.InteractOnOff, 0);
			_pressurizeToo = PressureExternal;
			ExteriorPoweredVent.ExternalPressure = _pressurizeToo;
			ExteriorPoweredVent.InternalPressure = PressurekPa.Zero;
			break;
		}
		while (IsOperable && AirlockControlState == transitState && _pressure < _pressurizeToo * 0.9900000095367432)
		{
			await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		}
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		switch (transitState)
		{
		case AdvancedAirlockState.PressurizingInternal:
			OnServer.Interact(InteriorPoweredVent.InteractOnOff, 0);
			OnServer.Interact(InteriorPoweredVent.InteractMode, 1);
			break;
		case AdvancedAirlockState.PressurizingExternal:
			OnServer.Interact(ExteriorPoweredVent.InteractOnOff, 0);
			OnServer.Interact(ExteriorPoweredVent.InteractMode, 1);
			break;
		}
		foreach (IPoweredVent poweredVent in base.PoweredVents)
		{
			OnServer.Interact(poweredVent.InteractOnOff, 0);
			OnServer.Interact(poweredVent.InteractMode, 1);
		}
		await UniTask.Delay(1000, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		if (!cancelToken.IsCancellationRequested)
		{
			switch (endState)
			{
			case AdvancedAirlockState.PressurizedInternal:
				OnServer.Interact(base.InteriorAirlock.InteractOpen, 1);
				break;
			case AdvancedAirlockState.PressurizedExternal:
				OnServer.Interact(base.ExteriorAirlock.InteractOpen, 1);
				break;
			}
			SetLights(isOn: false);
			SetSpeakers(isOn: false);
			UseComputerAfter(endState).Forget();
		}
	}

	private async UniTaskVoid UseComputerAfter(AdvancedAirlockState endState)
	{
		while (AirlockProcessing.Status == UniTaskStatus.Pending)
		{
			await UniTask.WaitForEndOfFrame(this.GetCancellationTokenOnDestroy());
		}
		Motherboard.UseComputer(3, base.netId, base.netId, (int)endState, sendToAll: true);
	}

	private async UniTask Depressurizing(AdvancedAirlockState transitState, AdvancedAirlockState endState)
	{
		if (!GameManager.RunSimulation || !IsOperable || (bool)base.MasterMotherboard)
		{
			RefreshScreen();
			return;
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		await WaitDoorClose(cancelToken);
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		switch (transitState)
		{
		case AdvancedAirlockState.DepressurizingInternal:
			InteriorPoweredVent.ExternalPressure = PressurekPa.Zero;
			InteriorPoweredVent.InternalPressure = DefaultPressureMax;
			OnServer.Interact(InteriorPoweredVent.InteractOnOff, 1);
			OnServer.Interact(InteriorPoweredVent.InteractMode, 1);
			OnServer.Interact(ExteriorPoweredVent.InteractOnOff, 0);
			break;
		case AdvancedAirlockState.DepressurizingExternal:
			ExteriorPoweredVent.ExternalPressure = PressurekPa.Zero;
			ExteriorPoweredVent.InternalPressure = DefaultPressureMax;
			OnServer.Interact(ExteriorPoweredVent.InteractOnOff, 1);
			OnServer.Interact(ExteriorPoweredVent.InteractMode, 1);
			OnServer.Interact(InteriorPoweredVent.InteractOnOff, 0);
			break;
		}
		float vacuumCountdown = 1f;
		while (IsOperable && AirlockControlState == transitState)
		{
			vacuumCountdown = ((!(_pressure <= AirlockControlBase.VacuumPressure)) ? 1f : (vacuumCountdown - 0.1f));
			if (vacuumCountdown <= 0f)
			{
				break;
			}
			await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
			if (cancelToken.IsCancellationRequested)
			{
				return;
			}
		}
		foreach (IPoweredVent poweredVent in base.PoweredVents)
		{
			poweredVent.ExternalPressure = Chemistry.OneAtmosphere;
			OnServer.Interact(poweredVent.InteractOnOff, 0);
			OnServer.Interact(poweredVent.InteractMode, 0);
		}
		UseComputerAfter(endState).Forget();
	}

	public void ButtonPressureInternal()
	{
		if (InputWindow.ShowInputPanel("Internal Pressure Setting (kPa)", PressureInternal.ToFloat().ToString("F0"), this, 32, TMP_InputField.ContentType.IntegerNumber))
		{
			InputWindow.OnSubmit += SetInternalPressure;
		}
	}

	public void ButtonPressureExternal()
	{
		if (InputWindow.ShowInputPanel("External Pressure Setting (kPa)", PressureExternal.ToFloat().ToString("F0"), this, 32, TMP_InputField.ContentType.IntegerNumber))
		{
			InputWindow.OnSubmit += SetExternalPressure;
		}
	}

	public void SetInternalPressure(string value, string value2)
	{
		Motherboard.UseComputer(19, base.netId, base.netId, int.Parse(value), sendToAll: false);
	}

	public void SetExternalPressure(string value, string value2)
	{
		Motherboard.UseComputer(18, base.netId, base.netId, int.Parse(value), sendToAll: false);
	}

	public void InitializeAirlock()
	{
		if ((bool)base.MasterMotherboard)
		{
			RefreshScreen();
			return;
		}
		if (!IsOperable && GameManager.RunSimulation)
		{
			AirlockControlState = AdvancedAirlockState.Disabled;
			if (ParentComputer != null && ParentComputer.AsThing().Error != 1)
			{
				OnServer.Interact(ParentComputer.AsThing(), InteractableType.Error, 1);
			}
			return;
		}
		if (GameManager.RunSimulation && AirlockControlState == AdvancedAirlockState.Disabled)
		{
			if (!base.InteriorAirlock.IsOpen && base.ExteriorAirlock.IsOpen)
			{
				AirlockControlState = AdvancedAirlockState.PressurizedExternal;
				foreach (IPoweredVent poweredVent in base.PoweredVents)
				{
					OnServer.Interact(poweredVent.InteractOnOff, 0);
				}
			}
			if (base.InteriorAirlock.IsOpen && !base.ExteriorAirlock.IsOpen)
			{
				AirlockControlState = AdvancedAirlockState.PressurizedInternal;
				foreach (IPoweredVent poweredVent2 in base.PoweredVents)
				{
					OnServer.Interact(poweredVent2.InteractOnOff, 0);
				}
			}
			if ((!base.InteriorAirlock.IsOpen && !base.ExteriorAirlock.IsOpen) || (base.InteriorAirlock.IsOpen && base.ExteriorAirlock.IsOpen))
			{
				if (AirlockProcessing.Status == UniTaskStatus.Pending)
				{
					throw new TaskSchedulerException(GetType().Name + " " + DisplayName + " is already processing, cannot initialize new task");
				}
				AirlockControlState = AdvancedAirlockState.PressurizedInternal;
				AirlockProcessing = Pressurizing(AdvancedAirlockState.PressurizingInternal, AdvancedAirlockState.PressurizedInternal);
			}
		}
		if (GameManager.RunSimulation && ParentComputer != null && Error != 0 && AirlockControlState != AdvancedAirlockState.Disabled)
		{
			OnServer.Interact(ParentComputer.AsThing(), InteractableType.Error, 1);
		}
	}

	public override void SetFlag(int page)
	{
		base.SetFlag(page);
		AirlockControlState = (AdvancedAirlockState)page;
	}

	public override void RefreshScreen()
	{
		base.RefreshScreen();
		switch (AirlockControlState)
		{
		case AdvancedAirlockState.Disabled:
			ButtonTextFirst.text = "ERROR";
			ButtonTextSecond.text = "IN CONFIG";
			ButtonCycle.colors = AirlockControlBase.DefaultColors;
			break;
		case AdvancedAirlockState.PressurizingInternal:
			ButtonTextFirst.text = "CANCEL";
			ButtonTextSecond.text = "PRESSURIZE";
			ButtonCycle.colors = AirlockControlBase.CancelColors;
			break;
		case AdvancedAirlockState.PressurizedInternal:
			ButtonTextFirst.text = "CYCLE";
			ButtonTextSecond.text = "TO EXTERIOR";
			ButtonCycle.colors = AirlockControlBase.DefaultColors;
			break;
		case AdvancedAirlockState.DepressurizingInternal:
			ButtonTextFirst.text = "CANCEL";
			ButtonTextSecond.text = "DEPRESSURIZE";
			ButtonCycle.colors = AirlockControlBase.CancelColors;
			break;
		case AdvancedAirlockState.PressurizingExternal:
			ButtonTextFirst.text = "CANCEL";
			ButtonTextSecond.text = "PRESSURIZE";
			ButtonCycle.colors = AirlockControlBase.CancelColors;
			break;
		case AdvancedAirlockState.PressurizedExternal:
			ButtonTextFirst.text = "CYCLE";
			ButtonTextSecond.text = "TO INTERIOR";
			ButtonCycle.colors = AirlockControlBase.DefaultColors;
			break;
		case AdvancedAirlockState.DepressurizingExternal:
			ButtonTextFirst.text = "CANCEL";
			ButtonTextSecond.text = "DEPRESSURIZE";
			ButtonCycle.colors = AirlockControlBase.CancelColors;
			break;
		}
		ButtonPressureExternalText.text = string.Format("External:\n<b>{0}</b>", (PressureExternal * 1000.0).ToFloat().ToStringPrefix("Pa"));
		ButtonPressureInternalText.text = string.Format("Internal:\n<b>{0}</b>", (PressureInternal * 1000.0).ToFloat().ToStringPrefix("Pa"));
	}

	public override async UniTask DeviceListChangeTask()
	{
		await base.DeviceListChangeTask();
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		await UniTask.NextFrame(cancelToken);
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		foreach (ComputerButton button in Buttons)
		{
			Door door = button.AssignedDevice as Door;
			if ((bool)door)
			{
				if (door == base.ExteriorAirlock || door == base.InteriorAirlock)
				{
					button.Label.text = button.Label.text + " <color=red><b>" + ((door == base.InteriorAirlock) ? "INTERIOR" : "EXTERIOR") + "</b></color>";
					continue;
				}
				if (!base.ExteriorAirlock || !base.InteriorAirlock)
				{
					button.Label.text = button.Label.text + " <color=green><b>" + (base.ExteriorAirlock ? "INTERIOR" : "EXTERIOR") + "</b></color>";
					continue;
				}
				button.Button.interactable = false;
			}
			if (button.AssignedDevice is IPoweredVent poweredVent)
			{
				if (poweredVent == ExteriorPoweredVent || poweredVent == InteriorPoweredVent)
				{
					button.Label.text = button.Label.text + " <color=red><b>" + ((poweredVent == InteriorPoweredVent) ? "INTERIOR" : "EXTERIOR") + "</b></color>";
				}
				else if (ExteriorPoweredVent == null || InteriorPoweredVent == null)
				{
					button.Label.text = button.Label.text + " <color=green><b>" + ((ExteriorPoweredVent != null) ? "INTERIOR" : "EXTERIOR") + "</b></color>";
				}
				continue;
			}
			WallLight wallLight = button.AssignedDevice as WallLight;
			if ((bool)wallLight)
			{
				button.Label.text = button.Label.text + " <color=" + (base.WarningLights.Contains(wallLight) ? "red" : "green") + "><b>LIGHT</b></color>";
				continue;
			}
			GasSensor gasSensor = button.AssignedDevice as GasSensor;
			if ((bool)gasSensor)
			{
				button.Label.text = button.Label.text + " <color=" + (base.GasSensors.Contains(gasSensor) ? "red" : "green") + "><b>SENSOR</b></color>";
			}
		}
		if ((bool)base.MasterMotherboard)
		{
			RefreshScreen();
		}
	}

	protected override List<long> MakeDeviceSaveRefs()
	{
		List<long> list = new List<long>();
		if (base.ExteriorAirlock != null)
		{
			list.Add(base.ExteriorAirlock.ReferenceId);
		}
		if (base.InteriorAirlock != null)
		{
			list.Add(base.InteriorAirlock.ReferenceId);
		}
		if (ExteriorPoweredVent != null)
		{
			list.Add(ExteriorPoweredVent.ReferenceId);
		}
		if (InteriorPoweredVent != null)
		{
			list.Add(InteriorPoweredVent.ReferenceId);
		}
		foreach (Device linkedDevice in base.LinkedDevices)
		{
			if (!list.Contains(linkedDevice.ReferenceId))
			{
				list.Add(linkedDevice.ReferenceId);
			}
		}
		return list;
	}

	public override void OnDeviceListChanged()
	{
		if ((bool)base.MasterMotherboard)
		{
			RefreshScreen();
			return;
		}
		lock (_lockGasSensors)
		{
			base.GasSensors.Clear();
		}
		if ((bool)base.ExteriorAirlock)
		{
			if (!base.LinkedDevices.Contains(base.ExteriorAirlock))
			{
				OnServer.Interact(base.ExteriorAirlock, InteractableType.Lock, 0);
				base.ExteriorAirlock = null;
			}
			else
			{
				bool flag = IsDeviceConnected(base.ExteriorAirlock);
				if (flag != base.ExteriorAirlock.IsLocked)
				{
					OnServer.Interact(base.ExteriorAirlock, InteractableType.Lock, flag ? 1 : 0);
				}
			}
		}
		if ((bool)base.InteriorAirlock)
		{
			if (!base.LinkedDevices.Contains(base.InteriorAirlock))
			{
				OnServer.Interact(base.InteriorAirlock, InteractableType.Lock, 0);
				base.InteriorAirlock = null;
			}
			else
			{
				bool flag2 = IsDeviceConnected(base.InteriorAirlock);
				if (flag2 != base.InteriorAirlock.IsLocked)
				{
					OnServer.Interact(base.InteriorAirlock, InteractableType.Lock, flag2 ? 1 : 0);
				}
			}
		}
		if (InteriorPoweredVent != null)
		{
			if (!base.LinkedDevices.Contains((Device)InteriorPoweredVent))
			{
				OnServer.Interact(InteriorPoweredVent.InteractLock, 0);
				InteriorPoweredVent = null;
			}
			else
			{
				bool flag3 = IsDeviceConnected((Device)InteriorPoweredVent);
				if (flag3 != InteriorPoweredVent.IsLocked)
				{
					OnServer.Interact(base.InteractLock, flag3 ? 1 : 0);
				}
			}
		}
		if (ExteriorPoweredVent != null)
		{
			if (!base.LinkedDevices.Contains((Device)ExteriorPoweredVent))
			{
				OnServer.Interact(base.InteractLock, 0);
				ExteriorPoweredVent = null;
			}
			else
			{
				bool flag4 = IsDeviceConnected((Device)ExteriorPoweredVent);
				if (flag4 != ExteriorPoweredVent.IsLocked)
				{
					OnServer.Interact(base.InteractLock, flag4 ? 1 : 0);
				}
			}
		}
		foreach (Device linkedDevice in base.LinkedDevices)
		{
			if (!IsDeviceConnected(linkedDevice))
			{
				continue;
			}
			Speaker speaker = linkedDevice as Speaker;
			if ((bool)speaker)
			{
				_speakers.Add(speaker);
				continue;
			}
			GasSensor gasSensor = linkedDevice as GasSensor;
			if ((bool)gasSensor)
			{
				lock (_lockGasSensors)
				{
					_gasSensors.Add(gasSensor);
				}
				continue;
			}
			if (linkedDevice is IPoweredVent poweredVent)
			{
				if (!base.PoweredVents.Contains(poweredVent))
				{
					base.PoweredVents.Add(poweredVent);
					if (_initialize)
					{
						OnServer.Interact(poweredVent.InteractOnOff, 0);
						OnServer.Interact(poweredVent.InteractLock, 1);
					}
				}
				if (ExteriorPoweredVent != poweredVent && InteriorPoweredVent != poweredVent)
				{
					if (ExteriorPoweredVent == null)
					{
						ExteriorPoweredVent = poweredVent;
					}
					else if (InteriorPoweredVent == null)
					{
						InteriorPoweredVent = poweredVent;
					}
				}
				continue;
			}
			WallLight wallLight = linkedDevice as WallLight;
			if ((bool)wallLight)
			{
				_lights.Add(wallLight);
				if (_initialize)
				{
					OnServer.Interact(wallLight.InteractOnOff, 0);
					OnServer.Interact(wallLight, InteractableType.Lock, 1);
				}
				continue;
			}
			Door door = linkedDevice as Door;
			if (!door || !(base.ExteriorAirlock != linkedDevice) || !(base.InteriorAirlock != linkedDevice))
			{
				continue;
			}
			if (!base.ExteriorAirlock)
			{
				if (!door.IsLocked)
				{
					OnServer.Interact(door, InteractableType.Lock, 1);
				}
				base.ExteriorAirlock = door;
			}
			else if (!base.InteriorAirlock)
			{
				if (!door.IsLocked)
				{
					OnServer.Interact(door, InteractableType.Lock, 1);
				}
				base.InteriorAirlock = door;
			}
		}
		int count = base.PoweredVents.Count;
		while (count-- > 0)
		{
			IPoweredVent poweredVent2 = base.PoweredVents[count];
			if (poweredVent2 != null && base.LinkedDevices.Contains((Device)poweredVent2))
			{
				continue;
			}
			base.PoweredVents.RemoveAt(count);
			if (GameManager.RunSimulation && poweredVent2 != null)
			{
				if (_initialize)
				{
					OnServer.Interact(poweredVent2.InteractOnOff, 0);
					OnServer.Interact(poweredVent2.InteractLock, 0);
				}
				poweredVent2.ResetVent();
			}
		}
		int count2 = base.WarningLights.Count;
		while (count2-- > 0)
		{
			WallLight wallLight2 = base.WarningLights[count2];
			if (wallLight2 == null || !base.LinkedDevices.Contains(wallLight2))
			{
				base.WarningLights.RemoveAt(count2);
				if (GameManager.RunSimulation && wallLight2 != null && _initialize)
				{
					OnServer.Interact(wallLight2.InteractOnOff, 0);
					OnServer.Interact(wallLight2, InteractableType.Lock, 0);
				}
			}
		}
		int count3 = base.Speakers.Count;
		while (count3-- > 0)
		{
			Speaker speaker2 = base.Speakers[count3];
			if (speaker2 == null || !base.LinkedDevices.Contains(speaker2))
			{
				base.Speakers.RemoveAt(count3);
				if (GameManager.RunSimulation && speaker2 != null && _initialize)
				{
					OnServer.Interact(speaker2.InteractOnOff, 0);
					OnServer.Interact(speaker2, InteractableType.Lock, 0);
				}
			}
		}
		int count4 = base.GasSensors.Count;
		while (count4-- > 0)
		{
			GasSensor gasSensor2 = base.GasSensors[count4];
			if (gasSensor2 == null || !base.LinkedDevices.Contains(gasSensor2))
			{
				lock (_lockGasSensors)
				{
					base.GasSensors.RemoveAt(count4);
				}
			}
		}
		if (_initialize)
		{
			InitializeAirlock();
		}
		if (!IsOperable && GameManager.GameState == GameState.Running)
		{
			_initialize = true;
		}
		base.OnDeviceListChanged();
	}

	public override void ButtonCycleAirlock()
	{
		base.ButtonCycleAirlock();
		switch (AirlockControlState)
		{
		case AdvancedAirlockState.PressurizingInternal:
			Achievements.AchieveHurryUp();
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 3, sendToAll: true);
			break;
		case AdvancedAirlockState.PressurizedInternal:
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 3, sendToAll: true);
			break;
		case AdvancedAirlockState.DepressurizingInternal:
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 1, sendToAll: true);
			Achievements.AchieveHurryUp();
			break;
		case AdvancedAirlockState.PressurizingExternal:
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 6, sendToAll: true);
			Achievements.AchieveHurryUp();
			break;
		case AdvancedAirlockState.PressurizedExternal:
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 6, sendToAll: true);
			break;
		case AdvancedAirlockState.DepressurizingExternal:
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 4, sendToAll: true);
			Achievements.AchieveHurryUp();
			break;
		case AdvancedAirlockState.Disabled:
			break;
		}
	}
}
