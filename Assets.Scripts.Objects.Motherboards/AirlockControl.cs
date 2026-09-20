using System;
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
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Objects.Motherboards;

public class AirlockControl : AirlockControlBase
{
	public delegate void OnAirlockProgrammed(Thing thing);

	protected PressurekPa DefaultPressure = new PressurekPa(50662.5);

	private AirlockControlState _controlStateLastRefresh = AirlockControlState.None;

	public AirlockControlState AirlockControlState
	{
		get
		{
			return (AirlockControlState)base.Flag;
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
				if (AirlockControlState == AirlockControlState.Depressurizing)
				{
					AirlockProcessing = AirlockDepressurize();
				}
				if (AirlockControlState == AirlockControlState.Pressurizing)
				{
					AirlockProcessing = AirlockPressurize();
				}
			}
		}
	}

	public override bool IsError => AirlockControlState == AirlockControlState.Disabled;

	public override bool CanToggle
	{
		get
		{
			if (AirlockControlState != AirlockControlState.Pressurized)
			{
				return AirlockControlState == AirlockControlState.Depressurized;
			}
			return true;
		}
	}

	public static event OnAirlockProgrammed AirlockProgrammedEvent;

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt32((int)AirlockControlState);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			AirlockControlState = (AirlockControlState)reader.ReadInt32();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32((int)AirlockControlState);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		AirlockControlState = (AirlockControlState)reader.ReadInt32();
	}

	private async UniTask AirlockPressurize()
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
		foreach (IPoweredVent poweredVent in base.PoweredVents)
		{
			OnServer.Interact(poweredVent.InteractOnOff, 1);
			OnServer.Interact(poweredVent.InteractMode, 0);
		}
		while (IsOperable && AirlockControlState == AirlockControlState.Pressurizing && _pressure < Chemistry.OneAtmosphere * 0.9900000095367432)
		{
			await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		}
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		foreach (IPoweredVent poweredVent2 in base.PoweredVents)
		{
			OnServer.Interact(poweredVent2.InteractOnOff, 0);
			OnServer.Interact(poweredVent2.InteractMode, 1);
		}
		await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		if (!cancelToken.IsCancellationRequested)
		{
			OnServer.Interact(base.InteriorAirlock.InteractOpen, 1);
			SetLights(isOn: false);
			SetSpeakers(isOn: false);
			UseComputerAfter(AirlockControlState.Pressurized).Forget();
		}
	}

	private async UniTaskVoid UseComputerAfter(AirlockControlState endState)
	{
		while (AirlockProcessing.Status == UniTaskStatus.Pending)
		{
			await UniTask.WaitForEndOfFrame(this.GetCancellationTokenOnDestroy());
		}
		Motherboard.UseComputer(3, base.netId, base.netId, (int)endState, sendToAll: true);
	}

	protected async UniTask AirlockDepressurize()
	{
		if (!GameManager.RunSimulation || !IsOperable || (bool)base.MasterMotherboard)
		{
			RefreshScreen();
			return;
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		if (base.DoWarnings)
		{
			SetLights(isOn: true);
			SetSpeakers(isOn: true);
			await UniTask.Delay(2000, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		}
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		OnServer.Interact(base.ExteriorAirlock.InteractOpen, 0);
		OnServer.Interact(base.InteriorAirlock.InteractOpen, 0);
		await UniTask.Delay(200, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		foreach (IPoweredVent poweredVent in base.PoweredVents)
		{
			poweredVent.ExternalPressure = PressurekPa.Zero;
			OnServer.Interact(poweredVent.InteractOnOff, 1);
			OnServer.Interact(poweredVent.InteractMode, 1);
			poweredVent.InternalPressure = DefaultPressure;
		}
		float vacuumCountdown = 1f;
		while (IsOperable && AirlockControlState == AirlockControlState.Depressurizing)
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
		foreach (IPoweredVent poweredVent2 in base.PoweredVents)
		{
			poweredVent2.ExternalPressure = Chemistry.OneAtmosphere;
			OnServer.Interact(poweredVent2.InteractOnOff, 0);
			OnServer.Interact(poweredVent2.InteractMode, 0);
			poweredVent2.InternalPressure = PressurekPa.Zero;
		}
		await UniTask.Delay(1000, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		if (!cancelToken.IsCancellationRequested)
		{
			OnServer.Interact(base.ExteriorAirlock.InteractOpen, 1);
			SetLights(isOn: false);
			SetSpeakers(isOn: false);
			UseComputerAfter(AirlockControlState.Depressurized).Forget();
		}
	}

	public override void RefreshScreen()
	{
		base.RefreshScreen();
		if (_controlStateLastRefresh != AirlockControlState)
		{
			_controlStateLastRefresh = AirlockControlState;
			switch (AirlockControlState)
			{
			case AirlockControlState.Disabled:
				ButtonTextFirst.text = GameStrings.AirlockErrorState;
				ButtonTextSecond.text = GameStrings.AirlockInConfigString;
				ButtonCycle.colors = AirlockControlBase.DefaultColors;
				break;
			case AirlockControlState.Pressurizing:
				ButtonTextFirst.text = GameStrings.AirlockCancelButton;
				ButtonTextSecond.text = GameStrings.AirlockPressurize;
				ButtonCycle.colors = AirlockControlBase.CancelColors;
				break;
			case AirlockControlState.Pressurized:
				ButtonTextFirst.text = GameStrings.AirlockCycle;
				ButtonTextSecond.text = GameStrings.AirlockToExterior;
				ButtonCycle.colors = AirlockControlBase.DefaultColors;
				break;
			case AirlockControlState.Depressurizing:
				ButtonTextFirst.text = GameStrings.AirlockCancelButton;
				ButtonTextSecond.text = GameStrings.AirlockDepressurize;
				ButtonCycle.colors = AirlockControlBase.CancelColors;
				break;
			case AirlockControlState.Depressurized:
				ButtonTextFirst.text = GameStrings.AirlockCycle;
				ButtonTextSecond.text = GameStrings.AirlockToInterior;
				ButtonCycle.colors = AirlockControlBase.DefaultColors;
				break;
			case AirlockControlState.None:
				throw new Exception("AirlockControlState should never be set to None");
			default:
				throw new ArgumentOutOfRangeException();
			case AirlockControlState.OverrideCount:
			case AirlockControlState.Override:
				break;
			}
		}
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
			AirlockControlState = AirlockControlState.Disabled;
			if (ParentComputer != null && ParentComputer.AsThing().Error != 1)
			{
				OnServer.Interact(ParentComputer.AsThing(), InteractableType.Error, 1);
			}
			return;
		}
		if (GameManager.RunSimulation && AirlockControlState == AirlockControlState.Disabled)
		{
			if (!base.InteriorAirlock.IsOpen && base.ExteriorAirlock.IsOpen)
			{
				AirlockControlState = AirlockControlState.Depressurized;
				foreach (IPoweredVent poweredVent in base.PoweredVents)
				{
					OnServer.Interact(poweredVent.InteractOnOff, 0);
				}
			}
			if (base.InteriorAirlock.IsOpen && !base.ExteriorAirlock.IsOpen)
			{
				AirlockControlState = AirlockControlState.Pressurized;
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
				AirlockControlState = AirlockControlState.Pressurizing;
				AirlockProcessing = AirlockPressurize();
			}
		}
		if (GameManager.RunSimulation && ParentComputer != null && Error != 0 && AirlockControlState != AirlockControlState.Disabled)
		{
			OnServer.Interact(ParentComputer.AsThing(), InteractableType.Error, 1);
		}
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
			if (button.AssignedDevice is IPoweredVent item)
			{
				button.Label.text = button.Label.text + " <color=" + (base.PoweredVents.Contains(item) ? "red" : "green") + "><b>VENT</b></color>";
				continue;
			}
			WallLight wallLight = button.AssignedDevice as WallLight;
			if ((bool)wallLight)
			{
				button.Label.text = button.Label.text + " <color=" + (base.WarningLights.Contains(wallLight) ? "red" : "green") + "><b>LIGHT</b></color>";
				continue;
			}
			Speaker speaker = button.AssignedDevice as Speaker;
			if ((bool)speaker)
			{
				button.Label.text = button.Label.text + " <color=" + (base.Speakers.Contains(speaker) ? "red" : "green") + "><b>SPEAKER</b></color>";
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
		if ((bool)base.ExteriorAirlock)
		{
			list.Add(base.ExteriorAirlock.ReferenceId);
		}
		if ((bool)base.InteriorAirlock)
		{
			list.Add(base.InteriorAirlock.ReferenceId);
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
				AirlockControl.AirlockProgrammedEvent?.Invoke(gasSensor);
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
					AirlockControl.AirlockProgrammedEvent?.Invoke((Thing)poweredVent);
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
				AirlockControl.AirlockProgrammedEvent?.Invoke(base.ExteriorAirlock);
			}
			else if (!base.InteriorAirlock)
			{
				if (!door.IsLocked)
				{
					OnServer.Interact(door, InteractableType.Lock, 1);
				}
				base.InteriorAirlock = door;
				AirlockControl.AirlockProgrammedEvent?.Invoke(base.InteriorAirlock);
			}
		}
		int count = base.Speakers.Count;
		while (count-- > 0)
		{
			Speaker speaker2 = base.Speakers[count];
			if (speaker2 == null || !base.LinkedDevices.Contains(speaker2))
			{
				base.Speakers.RemoveAt(count);
				if (GameManager.RunSimulation && speaker2 != null && _initialize)
				{
					OnServer.Interact(speaker2.InteractOnOff, 0);
					OnServer.Interact(speaker2, InteractableType.Lock, 0);
				}
			}
		}
		int count2 = base.GasSensors.Count;
		while (count2-- > 0)
		{
			GasSensor gasSensor2 = base.GasSensors[count2];
			if (gasSensor2 == null || !base.LinkedDevices.Contains(gasSensor2))
			{
				lock (_lockGasSensors)
				{
					base.GasSensors.RemoveAt(count2);
				}
			}
		}
		int count3 = base.PoweredVents.Count;
		while (count3-- > 0)
		{
			IPoweredVent poweredVent2 = base.PoweredVents[count3];
			if (poweredVent2 != null && base.LinkedDevices.Contains((Device)poweredVent2))
			{
				continue;
			}
			base.PoweredVents.RemoveAt(count3);
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
		int count4 = base.WarningLights.Count;
		while (count4-- > 0)
		{
			WallLight wallLight2 = base.WarningLights[count4];
			if (wallLight2 == null || !base.LinkedDevices.Contains(wallLight2))
			{
				base.WarningLights.RemoveAt(count4);
				if (GameManager.RunSimulation && wallLight2 != null && _initialize)
				{
					OnServer.Interact(wallLight2.InteractOnOff, 0);
					OnServer.Interact(wallLight2, InteractableType.Lock, 0);
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

	public override void SetFlag(int page)
	{
		base.SetFlag(page);
		AirlockControlState = (AirlockControlState)page;
	}

	public override void ButtonCycleAirlock()
	{
		base.ButtonCycleAirlock();
		if (AirlockControlState == AirlockControlState.Pressurized)
		{
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 3, sendToAll: true);
		}
		else if (AirlockControlState == AirlockControlState.Depressurized)
		{
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 1, sendToAll: true);
		}
		else if (AirlockControlState == AirlockControlState.Depressurizing)
		{
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 1, sendToAll: true);
			Achievements.AchieveHurryUp();
		}
		else if (AirlockControlState == AirlockControlState.Pressurizing)
		{
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 3, sendToAll: true);
			Achievements.AchieveHurryUp();
		}
	}

	public override void ButtonEmergencyOverride()
	{
		base.ButtonEmergencyOverride();
	}

	public override string GetStateString()
	{
		return AirlockControlState.GetName();
	}
}
