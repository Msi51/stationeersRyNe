using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Structures;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Objects.Motherboards;

public class AirlockControlBase : Circuitboard
{
	public static List<Device> AllAirlockEnabledPrefabs = new List<Device>();

	public const int WARNING_DELAY = 2000;

	public const int DOOR_DELAY = 200;

	public const int BUSY_DELAY = 100;

	public const int END_DELAY = 100;

	protected const float VACUUM_COUNTDOWN = 1f;

	private const float VACUUM_PRESSURE = 0.005f;

	public static readonly PressurekPa VacuumPressure = new PressurekPa(0.004999999888241291);

	[Header("Airlock Control")]
	[Tooltip("Main button. Changes color.")]
	public Button ButtonCycle;

	[Tooltip("Slider that gives visual indication of pressure state")]
	public Slider PressureSlider;

	[Tooltip("Text that displays the current pressure")]
	public Text PressureText;

	[Tooltip("Affects how fast the needle will move towards the current pressure")]
	public float LerpSpeed = 4f;

	[Tooltip("Color when pressure crtically low")]
	public Color PressureVaccumColor = Color.red;

	[Tooltip("Color when pressure somewhat low")]
	public Color PressureCautionColor = Color.yellow;

	[Tooltip("Color when pressure at or above one atmosphere")]
	public Color PressureSafeColor = Color.green;

	public Button ButtonOverride;

	public Text ButtonOverrideText;

	public Text ButtonTextFirst;

	public Text ButtonTextSecond;

	public static ColorBlock DefaultColors;

	public static ColorBlock CancelColors;

	protected bool _initialize;

	protected Image _pressureSliderColor;

	protected readonly List<WallLight> _lights = new List<WallLight>();

	protected readonly List<GasSensor> _gasSensors = new List<GasSensor>();

	protected readonly List<IPoweredVent> _PoweredVents = new List<IPoweredVent>();

	protected readonly List<Speaker> _speakers = new List<Speaker>();

	public Event OnAirlockOperable;

	private Door _exteriorAirlock;

	private Door _interiorAirlock;

	public static bool IsColorSet;

	protected PressurekPa _pressure;

	protected PressurekPa _displayPressure;

	private StateInstanceOld _pressureState;

	[ReadOnly]
	public UniTask AirlockProcessing;

	protected readonly object _lockGasSensors = new object();

	protected float _ratio;

	private bool _isNullText;

	protected bool DoWarnings
	{
		get
		{
			if (WarningLights.Count <= 0)
			{
				return Speakers.Count > 0;
			}
			return true;
		}
	}

	public virtual bool CanToggle => false;

	public List<WallLight> WarningLights
	{
		get
		{
			if (!(base.MasterMotherboard is AirlockControl))
			{
				return _lights;
			}
			return ((AirlockControl)base.MasterMotherboard).WarningLights;
		}
	}

	public List<GasSensor> GasSensors
	{
		get
		{
			if (!(base.MasterMotherboard is AirlockControl))
			{
				return _gasSensors;
			}
			return ((AirlockControl)base.MasterMotherboard).GasSensors;
		}
	}

	public List<IPoweredVent> PoweredVents
	{
		get
		{
			if (!(base.MasterMotherboard is AirlockControl))
			{
				return _PoweredVents;
			}
			return ((AirlockControl)base.MasterMotherboard).PoweredVents;
		}
	}

	public List<Speaker> Speakers
	{
		get
		{
			if (!(base.MasterMotherboard is AirlockControl))
			{
				return _speakers;
			}
			return ((AirlockControl)base.MasterMotherboard).Speakers;
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
			int num;
			if ((bool)ExteriorAirlock && ExteriorAirlock.IsLocked && (bool)InteriorAirlock && InteriorAirlock.IsLocked && GasSensors.Count > 0)
			{
				num = ((PoweredVents.Count > 0) ? 1 : 0);
				if (num != 0 && OnAirlockOperable != null)
				{
					OnAirlockOperable();
				}
			}
			else
			{
				num = 0;
			}
			return (byte)num != 0;
		}
	}

	public Door ExteriorAirlock
	{
		get
		{
			if (!(base.MasterMotherboard is AirlockControl))
			{
				return _exteriorAirlock;
			}
			return ((AirlockControl)base.MasterMotherboard).ExteriorAirlock;
		}
		set
		{
			if ((bool)(base.MasterMotherboard as AirlockControl))
			{
				((AirlockControl)base.MasterMotherboard)._exteriorAirlock = value;
			}
			else
			{
				_exteriorAirlock = value;
			}
		}
	}

	public Door InteriorAirlock
	{
		get
		{
			if (!(base.MasterMotherboard is AirlockControl))
			{
				return _interiorAirlock;
			}
			return ((AirlockControl)base.MasterMotherboard).InteriorAirlock;
		}
		set
		{
			if ((bool)(base.MasterMotherboard as AirlockControl))
			{
				((AirlockControl)base.MasterMotherboard)._interiorAirlock = value;
			}
			else
			{
				_interiorAirlock = value;
			}
		}
	}

	public PressurekPa AirPressure => _pressure;

	public virtual string GetStateString()
	{
		return "Invalid";
	}

	public override bool CanDeviceLink(Device device)
	{
		return device is IAirlockDevice;
	}

	public override void FlashCircuit()
	{
		base.FlashCircuit();
		_PoweredVents.Clear();
		_exteriorAirlock = null;
		_interiorAirlock = null;
		_gasSensors.Clear();
	}

	protected async UniTask WaitDoorClose(CancellationToken cancelToken)
	{
		if (DoWarnings)
		{
			SetLights(isOn: true);
			SetSpeakers(isOn: true);
			await UniTask.Delay(2000, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		}
		if (cancelToken.IsCancellationRequested)
		{
			return;
		}
		OnServer.Interact(ExteriorAirlock.InteractOpen, 0);
		OnServer.Interact(InteriorAirlock.InteractOpen, 0);
		await UniTask.Delay(200, ignoreTimeScale: false, PlayerLoopTiming.Update, cancelToken);
		if (!cancelToken.IsCancellationRequested)
		{
			while (ExteriorAirlock.IsDeviceActive || InteriorAirlock.IsDeviceActive)
			{
				await UniTask.NextFrame(cancelToken);
			}
		}
	}

	public void SetLights(bool isOn)
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		foreach (WallLight warningLight in WarningLights)
		{
			OnServer.Interact(warningLight.InteractOnOff, isOn ? 1 : 0);
		}
	}

	public void SetSpeakers(bool isOn)
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		foreach (Speaker speaker in Speakers)
		{
			OnServer.Interact(speaker.InteractOnOff, isOn ? 1 : 0);
		}
	}

	public override void Awake()
	{
		base.Awake();
		_pressureState = new StateInstanceOld(PressureText, Unit.kPa);
		_pressureSliderColor = PressureSlider.fillRect.GetComponent<Image>();
		if (!IsColorSet)
		{
			DefaultColors = ButtonCycle.colors;
			CancelColors = ButtonCycle.colors;
			CancelColors.normalColor = PressureCautionColor;
			Color pressureCautionColor = PressureCautionColor;
			pressureCautionColor.r *= 1.5f;
			pressureCautionColor.g *= 1.5f;
			pressureCautionColor.b *= 1.5f;
			CancelColors.highlightedColor = pressureCautionColor;
			pressureCautionColor.r *= 1.5f;
			pressureCautionColor.g *= 1.5f;
			pressureCautionColor.b *= 1.5f;
			CancelColors.pressedColor = pressureCautionColor;
			IsColorSet = true;
		}
		_initialize = GameManager.GameState == GameState.Running && GameManager.RunSimulation;
	}

	public virtual void ButtonCycleAirlock()
	{
	}

	public virtual void ButtonEmergencyOverride()
	{
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		_pressure = PressurekPa.Zero;
		lock (_lockGasSensors)
		{
			if (GasSensors.Count == 0)
			{
				return;
			}
			foreach (GasSensor gasSensor in GasSensors)
			{
				_pressure += gasSensor.AirPressure;
			}
			_pressure /= (double)GasSensors.Count;
			if (_pressure.IsNaN())
			{
				_pressure = PressurekPa.Zero;
			}
			if (!GameManager.IsBatchMode)
			{
				_ratio = Mathf.Clamp01((_pressure / Chemistry.OneAtmosphere).ToFloat());
				if (float.IsNaN(_ratio))
				{
					_ratio = 0f;
				}
				_displayPressure = RocketMath.Lerp(_displayPressure, _pressure, LerpSpeed);
			}
		}
	}

	public override void UpdateEachFrame()
	{
		if (GameManager.IsBatchMode || WorldManager.IsGamePaused)
		{
			return;
		}
		base.UpdateEachFrame();
		if (IsOccluded || !PressureText.gameObject.activeInHierarchy)
		{
			return;
		}
		if (_pressureState.UpdateText(_displayPressure.ToFloat()))
		{
			if (_displayPressure < Chemistry.ArmstrongLimit)
			{
				_pressureSliderColor.color = PressureVaccumColor;
			}
			else if (_displayPressure < Chemistry.OneAtmosphere * 0.8999999761581421)
			{
				_pressureSliderColor.color = PressureCautionColor;
			}
			else
			{
				_pressureSliderColor.color = PressureSafeColor;
			}
		}
		PressureSlider.value = Mathf.Lerp(PressureSlider.value, _ratio, Time.deltaTime * LerpSpeed);
	}
}
