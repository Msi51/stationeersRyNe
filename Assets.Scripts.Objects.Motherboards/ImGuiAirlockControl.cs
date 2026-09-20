using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.UI.ImGuiUi;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class ImGuiAirlockControl : AirlockControlBase
{
	public delegate void OnAirlockProgrammed(Thing thing);

	protected PressurekPa _vaccumPressure = new PressurekPa(0.009999999776482582);

	protected PressurekPa _defaultPressure = new PressurekPa(50662.5);

	private int _id = -1;

	private float _lastFrameTime;

	private float _renderingFrameRate = 1f / 60f;

	[SerializeField]
	private RawImage _renderImage;

	private RenderTexture _renderTexture;

	private ImGuiWindowFlags _imGuiWindowFlags = (ImGuiWindowFlags)4391;

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

	public int Id => _id;

	public float LastFrameTime
	{
		get
		{
			return _lastFrameTime;
		}
		set
		{
			_lastFrameTime = value;
		}
	}

	public float RenderingFrameRate => _renderingFrameRate;

	public GraphicRaycaster GraphicRaycaster => ParentComputer?.GraphicRaycaster;

	public RawImage RenderImage => _renderImage;

	public RenderTexture RenderTexture
	{
		get
		{
			return _renderTexture;
		}
		set
		{
			_renderTexture = value;
		}
	}

	public static event OnAirlockProgrammed AirlockProgrammedEvent;

	public override void Awake()
	{
		base.Awake();
		Init();
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
			Motherboard.UseComputer(3, base.netId, base.netId, 2, sendToAll: true);
		}
	}

	protected virtual IEnumerator AirlockExternalPressurize()
	{
		yield return new WaitForSecondsRealtime(0f);
	}

	protected async UniTask AirlockDepressurize()
	{
		if (!GameManager.RunSimulation || !IsOperable || (bool)base.MasterMotherboard)
		{
			RefreshScreen();
			return;
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		SetLights(isOn: true);
		SetSpeakers(isOn: true);
		await UniTask.Delay(2000, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		OnServer.Interact(base.ExteriorAirlock.InteractOpen, 0);
		OnServer.Interact(base.InteriorAirlock.InteractOpen, 0);
		await UniTask.Delay(2000, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		foreach (IPoweredVent poweredVent in base.PoweredVents)
		{
			poweredVent.ExternalPressure = PressurekPa.Zero;
			OnServer.Interact(poweredVent.InteractOnOff, 1);
			OnServer.Interact(poweredVent.InteractMode, 1);
			poweredVent.InternalPressure = _defaultPressure;
		}
		while (IsOperable && AirlockControlState == AirlockControlState.Depressurizing && _pressure >= _vaccumPressure)
		{
			await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		}
		foreach (IPoweredVent poweredVent2 in base.PoweredVents)
		{
			poweredVent2.ExternalPressure = Chemistry.OneAtmosphere;
			OnServer.Interact(poweredVent2.InteractOnOff, 0);
			OnServer.Interact(poweredVent2.InteractMode, 0);
			poweredVent2.InternalPressure = PressurekPa.Zero;
		}
		await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		OnServer.Interact(base.ExteriorAirlock.InteractOpen, 1);
		SetLights(isOn: false);
		SetSpeakers(isOn: false);
		Motherboard.UseComputer(3, base.netId, base.netId, 4, sendToAll: true);
	}

	public override void RefreshScreen()
	{
		base.RefreshScreen();
		switch (AirlockControlState)
		{
		case AirlockControlState.Disabled:
			ButtonTextFirst.text = "ERROR";
			ButtonTextSecond.text = "IN CONFIG";
			ButtonCycle.colors = AirlockControlBase.DefaultColors;
			break;
		case AirlockControlState.Pressurizing:
			ButtonTextFirst.text = "CANCEL";
			ButtonTextSecond.text = "PRESSURIZE";
			ButtonCycle.colors = AirlockControlBase.CancelColors;
			break;
		case AirlockControlState.Pressurized:
			ButtonTextFirst.text = "CYCLE";
			ButtonTextSecond.text = "TO EXTERIOR";
			ButtonCycle.colors = AirlockControlBase.DefaultColors;
			break;
		case AirlockControlState.Depressurizing:
			ButtonTextFirst.text = "CANCEL";
			ButtonTextSecond.text = "DEPRESSURIZE";
			ButtonCycle.colors = AirlockControlBase.CancelColors;
			break;
		case AirlockControlState.Depressurized:
			ButtonTextFirst.text = "CYCLE";
			ButtonTextSecond.text = "TO INTERIOR";
			ButtonCycle.colors = AirlockControlBase.DefaultColors;
			break;
		}
	}

	public void InitializeAirlock()
	{
		if ((bool)base.MasterMotherboard)
		{
			RefreshScreen();
			return;
		}
		if (!IsOperable)
		{
			AirlockControlState = AirlockControlState.Disabled;
			if (ParentComputer != null && ParentComputer.AsThing().Error != 1 && GameManager.RunSimulation)
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
		await UniTask.WaitForEndOfFrame(cancelToken);
		await UniTask.WaitForEndOfFrame(cancelToken);
		foreach (ComputerButton button in Buttons)
		{
			Door door = button.AssignedDevice as Door;
			if ((bool)door)
			{
				if (door == base.ExteriorAirlock || door == base.InteriorAirlock)
				{
					button.Label.text = string.Format("{0} <color=red><b>{1}</b></color>", button.Label.text, (door == base.InteriorAirlock) ? "INTERIOR" : "EXTERIOR");
					continue;
				}
				if (!base.ExteriorAirlock || !base.InteriorAirlock)
				{
					button.Label.text = string.Format("{0} <color=green><b>{1}</b></color>", button.Label.text, base.ExteriorAirlock ? "INTERIOR" : "EXTERIOR");
					continue;
				}
				button.Button.interactable = false;
			}
			if (button.AssignedDevice is IPoweredVent item)
			{
				button.Label.text = string.Format("{0} <color={1}><b>VENT</b></color>", button.Label.text, base.PoweredVents.Contains(item) ? "red" : "green");
				continue;
			}
			WallLight wallLight = button.AssignedDevice as WallLight;
			if ((bool)wallLight)
			{
				button.Label.text = string.Format("{0} <color={1}><b>LIGHT</b></color>", button.Label.text, base.WarningLights.Contains(wallLight) ? "red" : "green");
				continue;
			}
			Speaker speaker = button.AssignedDevice as Speaker;
			if ((bool)speaker)
			{
				button.Label.text = string.Format("{0} <color={1}><b>SPEAKER</b></color>", button.Label.text, base.Speakers.Contains(speaker) ? "red" : "green");
				continue;
			}
			GasSensor gasSensor = button.AssignedDevice as GasSensor;
			if ((bool)gasSensor)
			{
				button.Label.text = string.Format("{0} <color={1}><b>SENSOR</b></color>", button.Label.text, base.GasSensors.Contains(gasSensor) ? "red" : "green");
			}
		}
		if ((bool)base.MasterMotherboard)
		{
			RefreshScreen();
		}
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
				Debug.LogWarning("External Door Unselected");
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
				Debug.LogWarning("Internal Door Unselected");
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
				ImGuiAirlockControl.AirlockProgrammedEvent?.Invoke(gasSensor);
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
					ImGuiAirlockControl.AirlockProgrammedEvent?.Invoke((Thing)poweredVent);
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
				Debug.LogWarning("External door Selected");
				ImGuiAirlockControl.AirlockProgrammedEvent?.Invoke(base.ExteriorAirlock);
			}
			else if (!base.InteriorAirlock)
			{
				if (!door.IsLocked)
				{
					OnServer.Interact(door, InteractableType.Lock, 1);
				}
				base.InteriorAirlock = door;
				Debug.LogWarning("Internal door Selected");
				ImGuiAirlockControl.AirlockProgrammedEvent?.Invoke(base.InteriorAirlock);
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
		}
		else if (AirlockControlState == AirlockControlState.Pressurizing)
		{
			Motherboard.UseComputer(3, base.MasterMotherboard ? base.MasterMotherboard.netId : base.netId, base.netId, 3, sendToAll: true);
		}
	}

	public override void ButtonEmergencyOverride()
	{
		base.ButtonEmergencyOverride();
	}

	public override string GetStateString()
	{
		return AirlockControlState.ToString();
	}

	public void Draw()
	{
		ImGui.GetBackgroundDrawList().AddCircleFilled(ImGui.GetIO().MousePos, 10f, ImGuiColor.Integer.Yellow);
		ImGui.SetNextWindowPos(Vector2.zero);
		ImGui.SetNextWindowSize(new Vector2(_renderTexture.width, _renderTexture.height));
		if (ImGui.Begin($"DeviceWindow{Id}", _imGuiWindowFlags))
		{
			ImGui.PushID(Id);
			ImGui.SetWindowFontScale(4f);
			if (ImGui.Button("Cycle Airlock"))
			{
				ButtonCycleAirlock();
			}
			ImGui.ProgressBar(PressureSlider.value);
			ImGui.PopID();
		}
	}

	public void Action()
	{
		if (!GameManager.IsBatchMode)
		{
			PressureSlider.value = Mathf.Lerp(PressureSlider.value, _ratio, Time.deltaTime * LerpSpeed);
		}
	}

	public void Init()
	{
		_renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
		_renderTexture.Create();
	}
}
