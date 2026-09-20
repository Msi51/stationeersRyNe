using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class NewWorldSummary : UserInterfaceBase
{
	public WorldDescription Parent;

	[FormerlySerializedAs("ParentBody")]
	public TMP_Text SummaryText;

	private WorldSetting _worldSetting;

	public void Refresh()
	{
		if (_worldSetting != null)
		{
			SetWorld(_worldSetting);
		}
	}

	public void SetWorld(WorldSetting worldSetting)
	{
		StringBuilder stringBuilder = new StringBuilder();
		_worldSetting = worldSetting;
		OrbitalSimulation simulation = OrbitalSimulation.GetSimulation(worldSetting, WorldConfigurationMenu.GetDayLengthSeconds());
		stringBuilder.AppendLine(GameStrings.WorldInfoPlanetaryBody.AsString(simulation.PlayerBody.OrbitingBody.ToTooltip()));
		string arg = (worldSetting.Gravity / -9.8f * 100f).ToStringPercent("yellow");
		if (float.IsNaN(worldSetting.Gravity) || Mathf.Abs(worldSetting.Gravity) <= float.Epsilon)
		{
			stringBuilder.AppendLine(GameStrings.WorldInfoZeroGravity.DisplayString);
		}
		else
		{
			stringBuilder.AppendLine(GameStrings.WorldInfoGravity.AsString(arg));
		}
		ValueRange solarEnergy = simulation.GetSolarEnergy();
		List<WeatherEvent> weatherEvents = worldSetting.WeatherEvents;
		if (weatherEvents != null && weatherEvents.Count > 0)
		{
			foreach (WeatherEvent weatherEvent in worldSetting.WeatherEvents)
			{
				if (weatherEvent != null && weatherEvent.IsValid())
				{
					stringBuilder.AppendLine(GameStrings.WorldInfoPlanetWeather.AsString(weatherEvent.Name.AsColor("lightblue")));
				}
			}
		}
		ValueRange valueRange = null;
		if (worldSetting.SolarAngleTemperatureCurve != null)
		{
			valueRange = new ValueRange(worldSetting.Data.GlobalAtmosphereData, 180f, 1f, solarEnergy);
		}
		MoleQuantity quantity = GlobalGasMix.Create(worldSetting.Data.GlobalAtmosphereData).TotalQuantityGas();
		VolumeLitres volume = worldSetting.Data.GlobalAtmosphereData.GetVolume();
		ValueRange valueRange2 = null;
		if (valueRange != null)
		{
			valueRange2 = new ValueRange(IdealGas.Pressure(quantity, new TemperatureKelvin(valueRange.Minimum), volume).ToFloat(), IdealGas.Pressure(quantity, new TemperatureKelvin(valueRange.Maximum), volume).ToFloat());
		}
		stringBuilder.AppendLine(GameStrings.WorldInfoRotationPeriod.AsString(simulation.PrimaryBody.ToTooltip(), new TimeLength(1200.0).ToNearestString().ToString("yellow"), simulation.GetSolarAngle(worldSetting).ToStringPrefix("°", "yellow", adaptive: false)));
		stringBuilder.AppendLine(GameStrings.WorldInfoSiderealPeriod.AsString(simulation.PrimaryBody.ToTooltip(), simulation.PlayerBody.GetSiderealYearLength().ToNearestString().ToString("yellow")));
		if (solarEnergy.Minimum <= float.Epsilon && solarEnergy.Maximum <= float.Epsilon)
		{
			stringBuilder.AppendLine(GameStrings.WorldInfoNoSolarEnergy.AsString(simulation.PrimaryBody.ToTooltip()));
		}
		else if (Mathf.Abs(solarEnergy.Minimum - solarEnergy.Maximum) < 0.01f)
		{
			stringBuilder.AppendLine(GameStrings.WorldInfoSolarEnergy.AsString(simulation.PrimaryBody.ToTooltip(), solarEnergy.Minimum.ToStringPrefix("W/m2", "yellow")));
		}
		else
		{
			stringBuilder.AppendLine(GameStrings.WorldInfoSolarEnergyRange.AsString(simulation.PrimaryBody.ToTooltip(), solarEnergy.Minimum.ToStringRounded("yellow") ?? "", solarEnergy.Maximum.ToStringRounded("yellow") + " W/m2"));
		}
		if (quantity.IsDenormalOrZero())
		{
			stringBuilder.AppendLine(GameStrings.WorldInfoIsVacuum.DisplayString);
		}
		else if (valueRange != null && valueRange2 != null)
		{
			if (Mathf.Abs(valueRange.Minimum - valueRange.Maximum) < 0.01f)
			{
				stringBuilder.AppendLine(GameStrings.WorldInfoTemperature.AsString(Parent.GetTempText(valueRange.Minimum) + " °C"));
			}
			else
			{
				stringBuilder.AppendLine(GameStrings.WorldInfoTemperatureRange.AsString(Parent.GetTempText(valueRange.Minimum) ?? "", Parent.GetTempText(valueRange.Maximum) + " °C"));
			}
			if (Mathf.Abs(valueRange2.Minimum - valueRange2.Maximum) < 0.01f)
			{
				stringBuilder.AppendLine(GameStrings.WorldInfoPressure.AsString(Parent.GetPressureText(valueRange2.Minimum) + " kPa"));
			}
			else
			{
				stringBuilder.AppendLine(GameStrings.WorldInfoPressureRange.AsString(Parent.GetPressureText(valueRange2.Minimum) ?? "", Parent.GetPressureText(valueRange2.Maximum) + " kPa"));
			}
		}
		SummaryText.alignment = TextAlignmentOptions.Right;
		SummaryText.text = stringBuilder.ToString();
	}

	public void SetText(string text)
	{
		SummaryText.alignment = TextAlignmentOptions.Left;
		SummaryText.text = text;
	}
}
