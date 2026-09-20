using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public class PowerGeneratorPipe : DeviceInputOutput, IThermal
{
	[Tooltip("Affects how fast the needle will move towards the current pressure")]
	public static float LerpSpeed = 2f;

	[Tooltip("Minimum degrees rotation on local Y")]
	public float NeedleMinimum;

	[Tooltip("Maximum degrees rotation on local Y")]
	public float NeedleMaximum = 280f;

	[Tooltip("The needle (required)")]
	public GameObject Needle;

	[Tooltip("The collider for tank display")]
	public Collider InfoPanel;

	private Transform _needleTransform;

	private float _lastAngleNeedle;

	private Quaternion _needleBaseRotation;

	private float _needleRotation;

	public static string[] GeneratorModeStrings = new string[2] { "Not Generating", "Generating" };

	[Tooltip("The volume of the internal atmosphere of the furnace")]
	[SerializeField]
	[FormerlySerializedAs("Volume")]
	public float volume = 10f;

	[FormerlySerializedAs("MinimumPressure")]
	[SerializeField]
	public float minimumPressure = 20000f;

	[FormerlySerializedAs("MinimumTemperature")]
	[SerializeField]
	public float minimumTemperature = 278.15f;

	[FormerlySerializedAs("MaximumTemperature")]
	[SerializeField]
	public float maximumTemperature = 328.15f;

	private float _pressureRatio;

	private float _pressureRating;

	public static float Efficiency = 0.17f;

	public Atmosphere WorldAtmosphere;

	private float _energyAsPower;

	private MoleEnergy _previousTotalEnergy = MoleEnergy.Zero;

	public float proceduralConvection = 0.01f;

	public int _ticksOver;

	public override float ConvectionFactor => proceduralConvection;

	public override float RadiationFactor => proceduralConvection;

	public override string[] ModeStrings => GeneratorModeStrings;

	public VolumeLitres Volume => new VolumeLitres(volume);

	public TemperatureKelvin MinimumTemperature => new TemperatureKelvin(minimumTemperature);

	public TemperatureKelvin MaximumTemperature => new TemperatureKelvin(maximumTemperature);

	public bool IsValidAtmosphere
	{
		get
		{
			if (WorldAtmosphere != null && WorldAtmosphere.PressureGassesAndLiquidsInPa > minimumPressure && WorldAtmosphere.Temperature > MinimumTemperature)
			{
				return WorldAtmosphere.Temperature < MaximumTemperature;
			}
			return false;
		}
	}

	public bool DoShutdown
	{
		get
		{
			if (WorldAtmosphere != null && !(WorldAtmosphere.PressureGassesAndLiquidsInPa < minimumPressure - 2f) && !(WorldAtmosphere.Temperature < MinimumTemperature - new TemperatureKelvin(2.0)))
			{
				return WorldAtmosphere.Temperature > MaximumTemperature + new TemperatureKelvin(2.0);
			}
			return true;
		}
	}

	public override bool HasReadableAtmosphere => true;

	public override bool AllowSetPower(CableNetwork cableNetwork)
	{
		return false;
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (cableNetwork != base.PowerCableNetwork)
		{
			return 0f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return _energyAsPower;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.PowerGeneration)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.PowerGeneration)
		{
			return _energyAsPower;
		}
		return base.GetLogicValue(logicType);
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None)
		{
			_needleTransform = Needle.transform;
			_needleBaseRotation = _needleTransform.localRotation;
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	public override void OnThreadUpdate()
	{
		if (GameManager.IsBatchMode)
		{
			return;
		}
		base.OnThreadUpdate();
		if (base.InternalAtmosphere != null)
		{
			PressurekPa pressurekPa = base.InternalAtmosphere.PressureGassesAndLiquids / base.PressurePerTick;
			if (pressurekPa.IsNaN())
			{
				pressurekPa = PressurekPa.Zero;
			}
			_needleRotation = Mathf.Lerp(NeedleMinimum, NeedleMaximum, pressurekPa.ToFloat());
		}
	}

	public override void UpdateEachFrame()
	{
		if (!GameManager.IsBatchMode && !WorldManager.IsGamePaused)
		{
			base.UpdateEachFrame();
			if (!IsOccluded && !IsCursor)
			{
				_lastAngleNeedle = Mathf.Lerp(_lastAngleNeedle, _needleRotation, Time.deltaTime * LerpSpeed);
				_needleTransform.localRotation = _needleBaseRotation;
				_needleTransform.Rotate(0f, 0f, _lastAngleNeedle, Space.Self);
			}
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		Tooltip.ToolTipStringBuilder.Clear();
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (passiveTooltip.Title.Equals(string.Empty))
		{
			passiveTooltip.Title = DisplayName;
		}
		if (hitCollider == InfoPanel)
		{
			passiveTooltip.Extended = AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere);
			return passiveTooltip;
		}
		if (!IsValidAtmosphere)
		{
			Tooltip.ToolTipStringBuilder.Append(ToTooltip() + " requires an atmosphere of " + minimumPressure.ToStringPrefix("Pa", "yellow") + " at " + MinimumTemperature.ToFloat().ToStringPrefix("K", "yellow") + " to " + MaximumTemperature.ToFloat().ToStringPrefix("K", "yellow"));
			passiveTooltip.Extended = Tooltip.ToolTipStringBuilder.ToString();
			return passiveTooltip;
		}
		return passiveTooltip;
	}

	public override void OnAtmosphericTick()
	{
		WorldAtmosphere = base.AtmosphericsController.GetAtmosphereLocal(base.WorldGrid);
		base.OnAtmosphericTick();
		if (base.IsOutputValid)
		{
			OutputNetwork.Atmosphere.Add(base.InternalAtmosphere.GasMixture);
			base.InternalAtmosphere.GasMixture.Reset();
		}
		if (!OnOff)
		{
			_ticksOver = 0;
			_energyAsPower = 0f;
			if (Powered)
			{
				OnServer.Interact(base.InteractPowered, 0);
			}
			return;
		}
		if (_energyAsPower > 0f)
		{
			if (!Powered)
			{
				OnServer.Interact(base.InteractPowered, 1);
			}
		}
		else if (Powered)
		{
			OnServer.Interact(base.InteractPowered, 0);
		}
		if (!IsOperable)
		{
			return;
		}
		if (DoShutdown)
		{
			_ticksOver++;
			if (_ticksOver > 4)
			{
				OnServer.Interact(base.InteractOnOff, 0);
				_ticksOver = 0;
				return;
			}
		}
		else
		{
			_ticksOver = 0;
		}
		if (InputNetwork.Atmosphere.PressureGassesAndLiquids < new PressurekPa(0.001))
		{
			OnServer.Interact(base.InteractOnOff, 0);
			_energyAsPower = 0f;
			_previousTotalEnergy = MoleEnergy.Zero;
			return;
		}
		MoleQuantity transferMoles = IdealGas.Quantity(base.PressurePerTick, base.InternalAtmosphere.Volume, InputNetwork.Atmosphere.Temperature);
		if (InputNetwork.Atmosphere.GasMixture.GetTotalMolesGassesAndLiquids < Chemistry.MINIMUM_VALID_TOTAL_MOLES)
		{
			transferMoles = InputNetwork.Atmosphere.GasMixture.GetTotalMolesGassesAndLiquids;
		}
		GasMixture gasMixture = InputNetwork.Atmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All);
		base.InternalAtmosphere.Add(gasMixture);
		base.InternalAtmosphere.Sparked = true;
		base.InternalAtmosphere.TryCombust(0.8999999761581421, force: true);
		proceduralConvection = 0.01f + base.InternalAtmosphere.PressureGassesAndLiquids.ToFloat() / 100f * 0.0066f;
		proceduralConvection *= 0.28f;
		_previousTotalEnergy = base.InternalAtmosphere.CombustionEnergy;
		_energyAsPower = (_previousTotalEnergy * Efficiency).ToFloat();
		if (OutputNetwork != null && (OnOff || base.InternalAtmosphere.CombustionEnergy > MoleEnergy.Zero))
		{
			base.InternalAtmosphere.GasMixture.RemoveEnergy(new MoleEnergy(_energyAsPower));
		}
	}

	public override void OnServerTick(float deltaTime)
	{
		base.OnServerTick(deltaTime);
		_ = IsOperable;
	}
}
