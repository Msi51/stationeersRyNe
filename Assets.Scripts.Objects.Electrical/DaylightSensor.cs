using System;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Weather;

namespace Assets.Scripts.Objects.Electrical;

public class DaylightSensor : Sensor, IDoorControl, ILightActivated, IDensePoolable
{
	public enum DaylightSensorMode
	{
		Default,
		Horizontal,
		Vertical
	}

	public static string[] DaylightSensorString = Enum.GetNames(typeof(DaylightSensorMode));

	private float _solarAngle;

	private float _horizontal;

	private float _vertical;

	private PassiveTooltip _cachedTooltip = new PassiveTooltip(true);

	public override string[] ModeStrings => DaylightSensorString;

	public override bool IsTriggered => HasLight;

	public float SolarAngle => _solarAngle;

	public float Horizontal => _horizontal;

	public float Vertical => _vertical;

	private float LocalSolarIrradiance
	{
		get
		{
			float solarRatioAt = WeatherManager.GetSolarRatioAt(base.Position.y);
			if (!HasLight)
			{
				return 0f;
			}
			return OrbitalSimulation.SolarIrradiance * solarRatioAt;
		}
	}

	public override void Awake()
	{
		base.Awake();
		_cachedTooltip = new PassiveTooltip(true);
		_cachedTooltip.Title = DisplayName;
		_cachedTooltip.State = SolarInfo();
	}

	public override void SetMotherboards(bool isTriggered)
	{
		foreach (Motherboard linkedMotherboard in LinkedMotherboards)
		{
			if (linkedMotherboard is Circuitboard circuitboard && circuitboard.ParentComputer.AsDevice().Powered)
			{
				circuitboard.RemoteToggle(isTriggered);
			}
		}
	}

	public override async UniTaskVoid OnHasSunlight()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (GameManager.RunSimulation && GameManager.GameState != GameState.None && !IsTriggered)
		{
			ActivateSensor();
		}
	}

	public override async UniTaskVoid OnLostSunlight()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (GameManager.RunSimulation && GameManager.GameState != GameState.None)
		{
			ResetSensor();
		}
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		Vector3 v = RocketMath.InverseTransformDirecton(OrbitalSimulation.WorldSunVector, Direction);
		v = v.yxz();
		RocketMath.CartesianToSpherical(out var azimuth, out var elevation, out var radius, v);
		_solarAngle = (DaylightSensorMode)Mode switch
		{
			DaylightSensorMode.Horizontal => 57.29578f * azimuth, 
			DaylightSensorMode.Vertical => 57.29578f * elevation, 
			_ => Vector3.Angle(Forward, OrbitalSimulation.WorldSunVector), 
		};
		RocketMath.CartesianToSphericalFixed(out azimuth, out elevation, out radius, v);
		_horizontal = 57.29578f * azimuth;
		_vertical = 57.29578f * elevation;
	}

	private void OnDrawGizmos()
	{
		RocketMath.CartesianToSphericalFixed(out var azimuth, out var elevation, out var _, OrbitalSimulation.WorldSunVector);
		DebugHelpers.DrawArrow(base.Position, RocketMath.SphericalToCartesian(azimuth, elevation, 100f), Color.green);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Mode)
		{
			if (!doAction)
			{
				return DelayedActionInstance.Success(interactable.ContextualName);
			}
			OnServer.Interact(interactable, (interactable.State + 1) % 3);
			return DelayedActionInstance.Success(interactable.ContextualName);
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType - 20 <= LogicType.Open || logicType == LogicType.SolarIrradiance)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.SolarAngle => SolarAngle, 
			LogicType.Horizontal => _horizontal, 
			LogicType.Vertical => _vertical, 
			LogicType.SolarIrradiance => LocalSolarIrradiance, 
			LogicType.Activate => HasLight ? 1 : 0, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	private string SolarInfo()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(GameStrings.DaylightSensorGridSunlight.AsString(StringGenerator.GetString(HasLight).AsColor("yellow")));
		stringBuilder.AppendLine(GameStrings.DaylightSensorSolarAngle.AsString(StringGenerator.GetString(Mathf.RoundToInt(_solarAngle), Unit.Degrees).AsColor("yellow")));
		stringBuilder.AppendLine(GameStrings.DaylightSensorSolarIrradiance.AsString(StringGenerator.GetString(Mathf.RoundToInt(LocalSolarIrradiance), Unit.Watts).AsColor("yellow")));
		stringBuilder.AppendLine(GameStrings.DaylightSensorHorizontal.AsString(StringGenerator.GetString(Mathf.RoundToInt(_horizontal), Unit.Degrees).AsColor("yellow")));
		stringBuilder.AppendLine(GameStrings.DaylightSensorVertical.AsString(StringGenerator.GetString(Mathf.RoundToInt(_vertical), Unit.Degrees).AsColor("yellow")));
		if (Mode >= 0 && Mode < DaylightSensorString.Length)
		{
			stringBuilder.AppendLine(GameStrings.DaylightSensorMode.AsString(((DaylightSensorMode)Mode).GetName().AsColor("yellow")));
		}
		else
		{
			stringBuilder.AppendLine(GameStrings.DaylightSensorModeError.AsString(((DaylightSensorMode)Mode).GetName().AsColor("red")));
		}
		return stringBuilder.ToString();
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (Time.frameCount % 30 != 0)
		{
			_cachedTooltip = new PassiveTooltip(true);
			_cachedTooltip.Title = DisplayName;
			_cachedTooltip.State = SolarInfo();
		}
		return _cachedTooltip;
	}
}
