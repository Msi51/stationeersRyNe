using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class GasDisplay : Circuitboard
{
	public delegate void SelectedGasItem(Thing Item);

	[Header("Gas Display UI Config")]
	public Text DisplayTitle;

	public Text DisplayLabel;

	public Text DisplayUnits;

	public Text ToggleModeButtonText;

	public GasDisplayMode DisplayMode;

	public float LerpSpeed = 4f;

	[ReadOnly]
	public List<GasSensor> GasSensors = new List<GasSensor>();

	public List<PipeAnalysizer> PipeAnalysizers = new List<PipeAnalysizer>();

	public List<GasTankStorage> GasTankStorages = new List<GasTankStorage>();

	public List<Structure> Structures = new List<Structure>();

	public static string[] GasDisplayModeStrings = Enum.GetNames(typeof(GasDisplayMode));

	private readonly string[] _displayUnits = new string[3] { "kPa", "MPa", "GPa" };

	private int _currentUnitIndex;

	private int _lastUnitIndex;

	private int _sensors;

	private PressurekPa _pressure;

	private TemperatureKelvin _temperature;

	private PressurekPa _displayPressure;

	private string _displayText;

	private bool _notANumber;

	private static string _unitColorString = "white";

	private static string _decimalPlace3 = "{0:0.000}";

	private static string _decimalPlace2 = "{0:0.00}";

	private static string _decimalPlace1 = "{0:0.0}";

	private static string _decimalPlace0 = "{0:0}";

	public override string[] ModeStrings => GasDisplayModeStrings;

	public override bool IsError
	{
		get
		{
			if (!base.IsError)
			{
				return _notANumber;
			}
			return true;
		}
	}

	public event Event DisplayModeType;

	public event SelectedGasItem OnGasItemSelected;

	public override bool CanDeviceLink(Device device)
	{
		if (!(device is GasTankStorage) && !(device is GasSensor) && !(device is PipeAnalysizer))
		{
			return device.InternalAtmosphere != null;
		}
		return true;
	}

	public override void Awake()
	{
		base.Awake();
		SetFlag(base.Flag);
	}

	public override void OnDeviceListChanged()
	{
		base.OnDeviceListChanged();
		lock (base.LinkedDevices)
		{
			GasSensors.Clear();
			PipeAnalysizers.Clear();
			Structures.Clear();
			GasTankStorages.Clear();
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
					this.OnGasItemSelected?.Invoke(gasSensor);
					continue;
				}
				PipeAnalysizer pipeAnalysizer = linkedDevice as PipeAnalysizer;
				if ((bool)pipeAnalysizer)
				{
					PipeAnalysizers.Add(pipeAnalysizer);
					this.OnGasItemSelected?.Invoke(pipeAnalysizer);
					continue;
				}
				GasTankStorage gasTankStorage = linkedDevice as GasTankStorage;
				if ((bool)gasTankStorage)
				{
					GasTankStorages.Add(gasTankStorage);
					this.OnGasItemSelected?.Invoke(gasTankStorage);
					continue;
				}
				Structure structure = linkedDevice;
				if ((bool)structure)
				{
					Structures.Add(structure);
					this.OnGasItemSelected?.Invoke(structure);
				}
			}
		}
	}

	public override void ButtonToggleMode()
	{
		Motherboard.UseComputer(3, base.netId, base.netId, (base.Flag == 0) ? 1 : 0, sendToAll: true);
	}

	public override void SetFlag(int page)
	{
		base.SetFlag(page);
		base.Flag = page;
		if (page == 0)
		{
			DisplayMode = GasDisplayMode.Pressure;
			DisplayTitle.text = GameStrings.GasDisplayModeTitlePressure;
			DisplayUnits.text = "kPa";
			_lastUnitIndex = Array.IndexOf(_displayUnits, DisplayUnits.text);
			ToggleModeButtonText.text = GameStrings.GasDisplayModeButton.AsString(GameStrings.GasDisplayModePressure);
			this.DisplayModeType?.Invoke();
		}
		else
		{
			DisplayMode = GasDisplayMode.Temperature;
			DisplayTitle.text = GameStrings.GasDisplayModeTitleTemperature;
			DisplayUnits.text = "°C";
			ToggleModeButtonText.text = GameStrings.GasDisplayModeButton.AsString(GameStrings.GasDisplayModeTemperature);
			this.DisplayModeType?.Invoke();
		}
	}

	public override void OnThreadUpdate()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		lock (base.LinkedDevices)
		{
			if (!ShouldDraw())
			{
				return;
			}
			if (base.LinkedDevices.Count == 0)
			{
				_displayText = "-";
				return;
			}
			_sensors = 0;
			switch (DisplayMode)
			{
			case GasDisplayMode.Pressure:
			{
				_pressure = PressurekPa.Zero;
				int count5 = GasSensors.Count;
				while (count5-- > 0)
				{
					GasSensor gasSensor2 = GasSensors[count5];
					if ((bool)gasSensor2 && ParentComputer != null && ParentComputer.DataCableNetwork != null && IsDeviceConnected(gasSensor2) && !(gasSensor2.AirPressure <= PressurekPa.Zero))
					{
						_pressure += gasSensor2.AirPressure;
						_sensors++;
					}
				}
				int count6 = PipeAnalysizers.Count;
				while (count6-- > 0)
				{
					PipeAnalysizer pipeAnalysizer2 = PipeAnalysizers[count6];
					if ((bool)pipeAnalysizer2 && ParentComputer != null && ParentComputer.DataCableNetwork != null && IsDeviceConnected(pipeAnalysizer2) && !(pipeAnalysizer2.PipePressure < PressurekPa.Zero))
					{
						_pressure += pipeAnalysizer2.PipePressure;
						_sensors++;
					}
				}
				int count7 = GasTankStorages.Count;
				while (count7-- > 0)
				{
					GasTankStorage gasTankStorage2 = GasTankStorages[count7];
					if ((bool)gasTankStorage2 && ParentComputer != null && ParentComputer.DataCableNetwork != null && IsDeviceConnected(gasTankStorage2) && !(gasTankStorage2.TankPressure < PressurekPa.Zero))
					{
						_pressure += gasTankStorage2.TankPressure;
						_sensors++;
					}
				}
				int count8 = Structures.Count;
				while (count8-- > 0)
				{
					Structure structure2 = Structures[count8];
					if (!(structure2 == null) && ParentComputer != null && ParentComputer.DataCableNetwork != null && structure2.InternalAtmosphere != null && !(structure2.InternalAtmosphere.PressureGassesAndLiquids < PressurekPa.Zero))
					{
						_pressure += structure2.InternalAtmosphere.PressureGassesAndLiquids;
						_sensors++;
					}
				}
				_pressure /= (double)_sensors;
				if (_pressure.IsNaN())
				{
					_displayText = GameStrings.NaN;
					if (!_notANumber)
					{
						_notANumber = true;
						ErrorCheckFromThread().Forget();
					}
					break;
				}
				_displayPressure = RocketMath.Lerp(_displayPressure, _pressure, LerpSpeed);
				_displayText = FormatDisplayPressure(_displayPressure.ToFloat());
				if (_notANumber)
				{
					_notANumber = false;
					ErrorCheckFromThread().Forget();
				}
				break;
			}
			case GasDisplayMode.Temperature:
			{
				_temperature = TemperatureKelvin.Zero;
				int count = GasSensors.Count;
				while (count-- > 0)
				{
					GasSensor gasSensor = GasSensors[count];
					if ((bool)gasSensor && ParentComputer != null && ParentComputer.DataCableNetwork != null && IsDeviceConnected(gasSensor) && !(gasSensor.AirTemperature < TemperatureKelvin.Zero))
					{
						_temperature += gasSensor.AirTemperature;
						_sensors++;
					}
				}
				int count2 = PipeAnalysizers.Count;
				while (count2-- > 0)
				{
					PipeAnalysizer pipeAnalysizer = PipeAnalysizers[count2];
					if ((bool)pipeAnalysizer && ParentComputer != null && ParentComputer.DataCableNetwork != null && IsDeviceConnected(pipeAnalysizer) && !(pipeAnalysizer.PipeTemperature < TemperatureKelvin.Zero))
					{
						_temperature += pipeAnalysizer.PipeTemperature;
						_sensors++;
					}
				}
				int count3 = GasTankStorages.Count;
				while (count3-- > 0)
				{
					GasTankStorage gasTankStorage = GasTankStorages[count3];
					if ((bool)gasTankStorage && ParentComputer != null && ParentComputer.DataCableNetwork != null && IsDeviceConnected(gasTankStorage) && !(gasTankStorage.TankTemperature < TemperatureKelvin.Zero))
					{
						_temperature += gasTankStorage.TankTemperature;
						_sensors++;
					}
				}
				int count4 = Structures.Count;
				while (count4-- > 0)
				{
					Structure structure = Structures[count4];
					if ((bool)structure && ParentComputer != null && ParentComputer.DataCableNetwork != null && structure.InternalAtmosphere != null && !(structure.InternalAtmosphere.Temperature < TemperatureKelvin.Zero))
					{
						_temperature += structure.InternalAtmosphere.Temperature;
						_sensors++;
					}
				}
				_temperature /= (double)_sensors;
				if (_temperature.IsNaN())
				{
					_displayText = "NAN";
					if (!_notANumber)
					{
						_notANumber = true;
						ErrorCheckFromThread().Forget();
					}
				}
				else
				{
					_displayText = ((_temperature <= Chemistry.Temperature.Minimum) ? "-" : (_temperature - Chemistry.Temperature.ZeroDegrees).ToFloat().ToString("F1"));
					if (_notANumber)
					{
						_notANumber = false;
						ErrorCheckFromThread().Forget();
					}
				}
				break;
			}
			}
		}
	}

	private async UniTaskVoid ErrorCheckFromThread()
	{
		await UniTask.SwitchToMainThread();
		ParentComputer?.CheckStatus();
	}

	private bool ShouldDraw()
	{
		if (ParentComputer != null && !ParentComputer.AsThing().IsOccluded && ParentComputer.AsThing().OnOff)
		{
			return ParentComputer.AsThing().Powered;
		}
		return false;
	}

	public override void UpdateEachFrame()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		base.UpdateEachFrame();
		if (!ShouldDraw())
		{
			return;
		}
		if (GameManager.RunSimulation)
		{
			if (_notANumber && Error == 0)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			if (!_notANumber && Error == 1 && IsOperable)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
		DisplayLabel.text = _displayText;
		if (_lastUnitIndex != _currentUnitIndex)
		{
			DisplayUnits.text = _displayUnits[_currentUnitIndex];
			_lastUnitIndex = _currentUnitIndex;
		}
	}

	public string FormatDisplayPressure(float value)
	{
		int num = 0;
		while (value >= 1000f)
		{
			value /= 1000f;
			num++;
		}
		_currentUnitIndex = num;
		int num2 = (int)Math.Floor(Math.Log10(value) + 1.0);
		if (num2 >= 4)
		{
			return string.Format(_decimalPlace0, value);
		}
		if (num2 >= 3)
		{
			return string.Format(_decimalPlace1, value);
		}
		if (num2 >= 2)
		{
			return string.Format(_decimalPlace2, value);
		}
		if (num2 >= 1)
		{
			return string.Format(_decimalPlace3, value);
		}
		return $"{value:0}";
	}
}
