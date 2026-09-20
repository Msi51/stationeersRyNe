using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Clothing;

[Serializable]
public class SuitConvectionData
{
	public const float NORMALISED_TEMPERATURE_CHANGE = 30f;

	private const float EUROPA_NIGHT_TEMPERATURE = 123f;

	private const float MARS_NIGHT_TEMPERATURE = 220f;

	private const float MAX_VENUS_TEMPERATURE = 737f;

	private const float MAX_VULCAN_TEMPERATURE = 950f;

	private const float MAX_TEMPERATURE = 2500f;

	[SerializeField]
	private float thermalSaturationTime = 300f;

	[SerializeField]
	private float minimumTemperatureC = RocketMath.KelvinToCelsius(Chemistry.Temperature.Minimum);

	[SerializeField]
	private float operatingColdTemperatureC = RocketMath.KelvinToCelsius(new TemperatureKelvin(123.0));

	[SerializeField]
	private float operatingHotTemperatureC = RocketMath.KelvinToCelsius(new TemperatureKelvin(950.0));

	[SerializeField]
	private float maximumTemperatureC = RocketMath.KelvinToCelsius(new TemperatureKelvin(2500.0));

	public static TemperatureKelvin DefaultTemperature => Chemistry.Temperature.TwentyDegrees;

	public static PressurekPa DefaultPressure => Chemistry.OneAtmosphere;

	public TemperatureKelvin MinSafeTemperature => Chemistry.Temperature.MINSuitSetting;

	public TemperatureKelvin MaxSafeTemperature => Chemistry.Temperature.MAXSuitSetting;

	public float SafeTime => thermalSaturationTime;

	public float OperatingTime => 60f;

	public float MinimumTime => 5f;

	public float ThermalSaturationTime => thermalSaturationTime;

	public TemperatureKelvin MinimumTemperature => RocketMath.CelsiusToKelvin(minimumTemperatureC);

	public TemperatureKelvin OperatingColdTemperature => RocketMath.CelsiusToKelvin(operatingColdTemperatureC);

	public TemperatureKelvin OperatingHotTemperature => RocketMath.CelsiusToKelvin(operatingHotTemperatureC);

	public TemperatureKelvin MaximumTemperature => RocketMath.CelsiusToKelvin(maximumTemperatureC);

	public float MinimumTemperatureC => minimumTemperatureC;

	public float OperatingColdTemperatureC => operatingColdTemperatureC;

	public float OperatingHotTemperatureC => operatingHotTemperatureC;

	public float MaximumTemperatureC => maximumTemperatureC;

	public float GetSuitConvectionTime(TemperatureKelvin externalTemperature)
	{
		if (externalTemperature < MinimumTemperature)
		{
			return MinimumTime;
		}
		if (externalTemperature < OperatingColdTemperature)
		{
			return RocketMath.MapToScale(MinimumTemperature.ToFloat(), OperatingColdTemperature.ToFloat(), MinimumTime, OperatingTime, externalTemperature.ToFloat());
		}
		if (externalTemperature < MinSafeTemperature)
		{
			return RocketMath.MapToScale(OperatingColdTemperature.ToFloat(), MinSafeTemperature.ToFloat(), OperatingTime, SafeTime, externalTemperature.ToFloat());
		}
		if (externalTemperature < MaxSafeTemperature)
		{
			return SafeTime;
		}
		if (externalTemperature < OperatingHotTemperature)
		{
			return RocketMath.MapToScale(MaxSafeTemperature.ToFloat(), OperatingHotTemperature.ToFloat(), SafeTime, OperatingTime, externalTemperature.ToFloat());
		}
		if (externalTemperature < MaximumTemperature)
		{
			return RocketMath.MapToScale(OperatingHotTemperature.ToFloat(), MaximumTemperature.ToFloat(), OperatingTime, MinimumTime, externalTemperature.ToFloat());
		}
		if (externalTemperature.ToFloat() < 2500f)
		{
			return RocketMath.MapToScale(MaximumTemperature.ToFloat(), 2500f, MinimumTime, 1f, externalTemperature.ToFloat());
		}
		return 1f;
	}
}
