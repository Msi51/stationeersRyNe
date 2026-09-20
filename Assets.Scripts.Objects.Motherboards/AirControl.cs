using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class AirControl : Circuitboard
{
	[Header("Air  Control")]
	public Text TextStatus;

	[Tooltip("Slider that gives visual indication of pressure state")]
	public Slider PressureSlider;

	public Image StatusToxin;

	public Image StatusTemp;

	public Image StatusOxygen;

	public Image StatusPressure;

	[ReadOnly]
	public List<AirControlVent> ActiveVents = new List<AirControlVent>();

	[ReadOnly]
	public List<GasSensor> GasSensors = new List<GasSensor>();

	[Tooltip("Affects how fast the needle will move towards the current pressure")]
	public float LerpSpeed = 4f;

	[Tooltip("Color when pressure crtically low")]
	public Color PressureVaccumColor = Color.red;

	[Tooltip("Color when pressure somewhat low")]
	public Color PressureCautionColor = Color.yellow;

	[Tooltip("Color when pressure at or above one atmosphere")]
	public Color PressureSafeColor = Color.green;

	public Text ToggleModeButtonText;

	public Sprite IconPressureLowCritical;

	public Sprite IconPressureLowCaution;

	public Sprite IconPressureHighCritical;

	public Sprite IconPressureHighCaution;

	private Sprite _iconPressureDefault;

	public Sprite IconOxygenCritical;

	public Sprite IconOxygenCaution;

	private Sprite _iconOxygenDefault;

	private Image _panelModeImage;

	private Image _pressureSliderColor;

	private PressurekPa _pressure;

	private PressurekPa _pressureO2;

	private PressurekPa _displayPressure;

	private bool _disabledFill;

	public bool HasGasSensors;

	public bool HasActiveVents;

	public PressurekPa PressureMax = Chemistry.OneAtmosphere * 1.0099999904632568;

	public PressurekPa PressureMin = Chemistry.OneAtmosphere * 0.9900000095367432;

	private static PressurekPa defaultPressure = new PressurekPa(50662.5);

	public AirControlMode ControlMode;

	private bool _overPressure;

	private bool _underPressure;

	public override bool IsError
	{
		get
		{
			if (ActiveVents.Count != 0)
			{
				return GasSensors.Count == 0;
			}
			return true;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(ActiveVents.Count);
		foreach (AirControlVent activeVent in ActiveVents)
		{
			writer.WriteInt64(activeVent.ReferenceId);
			writer.WriteByte((byte)activeVent.Direction);
		}
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		int num = reader.ReadInt32();
		ActiveVents.Clear();
		for (int i = 0; i < num; i++)
		{
			long referenceId = reader.ReadInt64();
			VentDirection direction = (VentDirection)reader.ReadByte();
			ActiveVents.Add(new AirControlVent
			{
				ReferenceId = referenceId,
				Direction = direction
			});
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is AirContolCircuitboardSaveData airContolCircuitboardSaveData)
		{
			airContolCircuitboardSaveData.AirControlVents = ActiveVents;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new AirContolCircuitboardSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData saveData)
	{
		base.DeserializeSave(saveData);
	}

	public override void OnMotherboardDeserialized(ThingSaveData saveData)
	{
		base.OnMotherboardDeserialized(saveData);
		if (!(saveData is AirContolCircuitboardSaveData airContolCircuitboardSaveData))
		{
			return;
		}
		foreach (AirControlVent record in ActiveVents)
		{
			int num = airContolCircuitboardSaveData.AirControlVents.FindIndex((AirControlVent p) => p.ReferenceId == record.ReferenceId);
			if (num > -1)
			{
				record.Direction = airContolCircuitboardSaveData.AirControlVents[num].Direction;
			}
		}
	}

	public override void LinkDevicesOnJoinComplete()
	{
		base.LinkDevicesOnJoinComplete();
		foreach (AirControlVent activeVent in ActiveVents)
		{
			activeVent.PoweredVent = Referencable.Find<IPoweredVent>(activeVent.ReferenceId);
		}
	}

	public override void Awake()
	{
		base.Awake();
		_pressureSliderColor = PressureSlider.fillRect.GetComponent<Image>();
		_panelModeImage = TextStatus.transform.parent.GetComponent<Image>();
		_iconPressureDefault = StatusPressure.sprite;
		_iconOxygenDefault = StatusOxygen.sprite;
		_panelModeImage.enabled = false;
	}

	public AirControlMode GetAirControlMode()
	{
		if (FlagGet(2))
		{
			return AirControlMode.Pressure;
		}
		if (FlagGet(4))
		{
			return AirControlMode.Draught;
		}
		return AirControlMode.Offline;
	}

	public override void ButtonToggleMode()
	{
		switch (ControlMode)
		{
		case AirControlMode.Offline:
			FlagClear(1);
			FlagAdd(2);
			break;
		case AirControlMode.Pressure:
			FlagClear(2);
			FlagAdd(4);
			break;
		case AirControlMode.Draught:
			FlagClear(4);
			FlagAdd(1);
			break;
		}
		Motherboard.UseComputer(3, base.netId, base.netId, base.Flag, sendToAll: true);
	}

	public override void UpdateEachFrame()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		base.UpdateEachFrame();
		if (IsOccluded || GameManager.IsBatchMode)
		{
			return;
		}
		_displayPressure = RocketMath.Lerp(_displayPressure, _pressure, Time.deltaTime * LerpSpeed);
		if (_displayPressure <= PressurekPa.Zero)
		{
			if (!_disabledFill)
			{
				PressureSlider.fillRect.gameObject.SetActive(value: false);
				_disabledFill = true;
			}
		}
		else if (_disabledFill)
		{
			PressureSlider.fillRect.gameObject.SetActive(value: true);
			_disabledFill = false;
		}
		if (GasSensors.Count > 0)
		{
			float b = Mathf.Clamp((_pressure / Chemistry.OneAtmosphere).ToFloat(), 0f, 1f);
			PressureSlider.value = Mathf.Lerp(PressureSlider.value, b, Time.deltaTime * LerpSpeed);
			if (_displayPressure < Chemistry.ArmstrongLimit)
			{
				_pressureSliderColor.color = PressureVaccumColor;
				StatusPressure.sprite = IconPressureLowCritical;
			}
			else if (_displayPressure < Chemistry.OneAtmosphere * 0.8999999761581421)
			{
				_pressureSliderColor.color = PressureCautionColor;
				StatusPressure.sprite = IconPressureLowCaution;
			}
			else if (_displayPressure >= Chemistry.OneAtmosphere * 0.8999999761581421 && _displayPressure < Chemistry.OneAtmosphere * 1.100000023841858)
			{
				_pressureSliderColor.color = PressureSafeColor;
				StatusPressure.sprite = _iconPressureDefault;
			}
			else if (_displayPressure < Chemistry.OneAtmosphere * 2.0)
			{
				_pressureSliderColor.color = PressureCautionColor;
				StatusPressure.sprite = IconPressureHighCaution;
			}
			else
			{
				_pressureSliderColor.color = PressureVaccumColor;
				StatusPressure.sprite = IconPressureHighCritical;
			}
			if (_pressureO2 < Chemistry.MinimumOxygenPartialPressure)
			{
				StatusOxygen.sprite = IconOxygenCritical;
			}
			else if (_pressureO2 < Chemistry.MinimumOxygenPartialPressure + new PressurekPa(5.0))
			{
				StatusOxygen.sprite = IconOxygenCaution;
			}
			else
			{
				StatusOxygen.sprite = _iconOxygenDefault;
			}
		}
	}

	public override bool CanDeviceLink(Device device)
	{
		if (!(device is GasSensor))
		{
			return device is IPoweredVent;
		}
		return true;
	}

	public override void OnDeviceListChanged()
	{
		base.OnDeviceListChanged();
		GasSensors.Clear();
		foreach (Device linkedDevice in base.LinkedDevices)
		{
			if (!IsDeviceConnected(linkedDevice))
			{
				continue;
			}
			GasSensor gasSensor = linkedDevice as GasSensor;
			if ((bool)gasSensor)
			{
				GasSensors.Add(gasSensor);
			}
			if (!(linkedDevice is IPoweredVent poweredVent))
			{
				continue;
			}
			AirControlVent activeVentRecord = GetActiveVentRecord(poweredVent);
			if (activeVentRecord == null)
			{
				activeVentRecord = new AirControlVent
				{
					PoweredVent = poweredVent,
					Direction = poweredVent.VentDirection,
					ReferenceId = poweredVent.ReferenceId
				};
				ActiveVents.Add(activeVentRecord);
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(poweredVent.InteractLock, 1);
				}
			}
		}
		int count = ActiveVents.Count;
		while (count-- > 0)
		{
			AirControlVent airControlVent = ActiveVents[count];
			if (airControlVent.PoweredVent != null && base.LinkedDevices.Contains((Device)airControlVent.PoweredVent))
			{
				continue;
			}
			ActiveVents.RemoveAt(count);
			if (GameManager.RunSimulation && airControlVent.PoweredVent != null)
			{
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(airControlVent.PoweredVent.InteractOnOff, 0);
					OnServer.Interact(airControlVent.PoweredVent.InteractLock, 0);
				}
				airControlVent.PoweredVent.ResetVent();
			}
		}
		HasGasSensors = GasSensors.Count > 0;
		HasActiveVents = ActiveVents.Count > 0;
		UpdateScreen();
		if (!base.BeingDestroyed && GameManager.RunSimulation && base.gameObject.activeSelf)
		{
			UpdateDevices().Forget();
		}
	}

	public async UniTaskVoid UpdateDevices()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		foreach (AirControlVent activeVent in ActiveVents)
		{
			switch (ControlMode)
			{
			case AirControlMode.Pressure:
				if (_pressure < PressureMax && _pressure > PressureMin)
				{
					if (GameManager.RunSimulation)
					{
						OnServer.Interact(activeVent.PoweredVent.InteractMode, 0);
						OnServer.Interact(activeVent.PoweredVent.InteractOnOff, 0);
					}
				}
				else if (GameManager.RunSimulation)
				{
					if (_pressure < Chemistry.OneAtmosphere)
					{
						OnServer.Interact(activeVent.PoweredVent.InteractMode, 0);
						activeVent.PoweredVent.InternalPressure = PressurekPa.Zero;
					}
					else
					{
						OnServer.Interact(activeVent.PoweredVent.InteractMode, 1);
						activeVent.PoweredVent.InternalPressure = defaultPressure;
					}
					activeVent.PoweredVent.ExternalPressure = Chemistry.OneAtmosphere;
					OnServer.Interact(activeVent.PoweredVent.InteractOnOff, 1);
				}
				break;
			case AirControlMode.Draught:
				if (activeVent.Direction == VentDirection.Inward)
				{
					activeVent.PoweredVent.ExternalPressure = Chemistry.OneAtmosphere * 0.9750000238418579;
					activeVent.PoweredVent.InternalPressure = defaultPressure;
					if (GameManager.RunSimulation)
					{
						OnServer.Interact(activeVent.PoweredVent.InteractMode, 1);
						OnServer.Interact(activeVent.PoweredVent.InteractOnOff, 1);
					}
				}
				else if (activeVent.Direction == VentDirection.Outward)
				{
					activeVent.PoweredVent.ExternalPressure = Chemistry.OneAtmosphere * 1.024999976158142;
					activeVent.PoweredVent.InternalPressure = PressurekPa.Zero;
					if (GameManager.RunSimulation)
					{
						OnServer.Interact(activeVent.PoweredVent.InteractMode, 0);
						OnServer.Interact(activeVent.PoweredVent.InteractOnOff, 1);
					}
				}
				break;
			case AirControlMode.None:
			case AirControlMode.Offline:
				activeVent.PoweredVent.ExternalPressure = Chemistry.OneAtmosphere;
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(activeVent.PoweredVent.InteractMode, (activeVent.Direction == VentDirection.Inward) ? 1 : 0);
					OnServer.Interact(activeVent.PoweredVent.InteractOnOff, 0);
				}
				break;
			}
		}
	}

	private void UpdateScreen()
	{
		switch (ControlMode)
		{
		default:
			_panelModeImage.enabled = false;
			TextStatus.text = GameStrings.AirControlStatusOffline;
			ToggleModeButtonText.text = GameStrings.AirControlModeButton.AsString(GameStrings.AirControlModeOffline);
			break;
		case AirControlMode.Pressure:
			_panelModeImage.enabled = true;
			_panelModeImage.color = Color.blue;
			TextStatus.text = GameStrings.AirControlStatusPressure;
			ToggleModeButtonText.text = GameStrings.AirControlModeButton.AsString(GameStrings.AirControlModePressure);
			break;
		case AirControlMode.Draught:
			_panelModeImage.enabled = true;
			_panelModeImage.color = Color.blue;
			TextStatus.text = GameStrings.AirControlStatusDraught;
			ToggleModeButtonText.text = GameStrings.AirControlModeButton.AsString(GameStrings.AirControlModeDraught);
			break;
		}
		if (ControlMode != AirControlMode.Offline && (!HasGasSensors || !HasActiveVents))
		{
			_panelModeImage.enabled = true;
			_panelModeImage.color = Color.red;
			TextStatus.text = GameStrings.AirControlStatusError;
		}
	}

	public override void SetFlag(int page)
	{
		base.SetFlag(page);
		base.Flag = page;
		ControlMode = GetAirControlMode();
		UpdateScreen();
		if (GameManager.RunSimulation)
		{
			UpdateDevices().Forget();
		}
	}

	private AirControlVent GetActiveVentRecord(IPoweredVent poweredVent)
	{
		int num = ActiveVents.FindIndex((AirControlVent record) => record.PoweredVent == poweredVent);
		if (num < 0)
		{
			return null;
		}
		return ActiveVents[num];
	}

	public override async UniTask DeviceListChangeTask()
	{
		await base.DeviceListChangeTask();
		if ((bool)base.MasterMotherboard)
		{
			return;
		}
		CancellationToken cancelToken = this.GetCancellationTokenOnDestroy();
		await UniTask.WaitForEndOfFrame(cancelToken);
		await UniTask.WaitForEndOfFrame(cancelToken);
		foreach (ComputerButton button in Buttons)
		{
			if (button.AssignedDevice is IPoweredVent vent)
			{
				button.Label.text = ActiveVentLabel(vent);
			}
			else if (button.AssignedDevice is GasSensor item)
			{
				button.Label.text = string.Format("{0} <color={1}><b>{2}</b></color>", button.Label.text, GasSensors.Contains(item) ? "red" : "green", GameStrings.AirControlSensor);
			}
		}
	}

	public override void OnInsertedToComputer(IComputer computer)
	{
		base.OnInsertedToComputer(computer);
		AtmosphericsManager.Instance.Register(this);
	}

	public override void OnRemovedFromComputer(IComputer computer)
	{
		base.OnRemovedFromComputer(computer);
		AtmosphericsManager.Instance.Deregister(this);
	}

	private void CalculatePressure()
	{
		_pressureO2 = PressurekPa.Zero;
		_pressure = PressurekPa.Zero;
		int count = GasSensors.Count;
		while (count-- > 0)
		{
			GasSensor gasSensor = GasSensors[count];
			_pressure += gasSensor.AirPressure;
			_pressureO2 += gasSensor.PartialPressureO2;
		}
		_pressure /= (double)GasSensors.Count;
		_pressureO2 /= (double)GasSensors.Count;
		if (_pressure.IsNaN())
		{
			_pressure = PressurekPa.Zero;
		}
		if (_pressureO2.IsNaN())
		{
			_pressureO2 = PressurekPa.Zero;
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		CalculatePressure();
		if (!_overPressure && _pressure > PressureMax)
		{
			_underPressure = false;
			_overPressure = true;
			UpdateDevices().Forget();
		}
		if (!_underPressure && _pressure < PressureMin)
		{
			_underPressure = true;
			_overPressure = false;
			UpdateDevices().Forget();
		}
		if ((_underPressure || _overPressure) && _pressure < PressureMax && _pressure > PressureMin)
		{
			_underPressure = false;
			_overPressure = false;
			UpdateDevices().Forget();
		}
	}

	private string ActiveVentLabel(IPoweredVent vent)
	{
		return string.Format("{0} <color={1}><b>{2} {3}</b></color>", ((Thing)vent).DisplayName, (GetActiveVentRecord(vent) != null) ? "red" : "green", vent.VentDirection.GetName(), GameStrings.AirControlVent);
	}

	public override void RefreshDevice(Device device)
	{
		base.RefreshDevice(device);
		ComputerButton computerButton = FindDeviceButton(device);
		if (computerButton != null && device is IPoweredVent vent)
		{
			computerButton.Label.text = ActiveVentLabel(vent);
		}
	}
}
