using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class GasSensor : SmallDevice, IAirlockDevice, ISmartRotatable
{
	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private int _lastFrameCheck;

	public Atmosphere WorldAtmosphere;

	public bool AirIgnited
	{
		get
		{
			FindAtmosphere();
			return WorldAtmosphere?.Sparked ?? false;
		}
	}

	public PressurekPa AirPressure
	{
		get
		{
			FindAtmosphere();
			return WorldAtmosphere?.PressureGassesAndLiquids ?? PressurekPa.Zero;
		}
	}

	public MoleQuantity TotalMoles
	{
		get
		{
			FindAtmosphere();
			return WorldAtmosphere?.TotalMoles ?? MoleQuantity.Zero;
		}
	}

	public VolumeLitres VolumeOfLiquid
	{
		get
		{
			FindAtmosphere();
			return WorldAtmosphere?.TotalVolumeLiquids ?? VolumeLitres.Zero;
		}
	}

	public TemperatureKelvin AirTemperature
	{
		get
		{
			FindAtmosphere();
			return WorldAtmosphere?.Temperature ?? TemperatureKelvin.Zero;
		}
	}

	public PressurekPa PartialPressureO2
	{
		get
		{
			FindAtmosphere();
			return WorldAtmosphere?.PartialPressureO2 ?? PressurekPa.Zero;
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		result.Extended = AtmosphericsManager.DisplayBasicAtmosphere(WorldAtmosphere);
		return result;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Pressure:
		case LogicType.Temperature:
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.TotalMoles:
		case LogicType.RatioNitrousOxide:
		case LogicType.Combustion:
		case LogicType.RatioLiquidNitrogen:
		case LogicType.VolumeOfLiquid:
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioSteam:
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
		case LogicType.RatioHydrazine:
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioHelium:
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioSilanol:
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioOzone:
		case LogicType.RatioLiquidOzone:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Combustion:
			if (!AirIgnited)
			{
				return 0.0;
			}
			return 1.0;
		case LogicType.Pressure:
			return AirPressure.ToDouble();
		case LogicType.Temperature:
			return AirTemperature.ToDouble();
		case LogicType.TotalMoles:
			return TotalMoles.ToDouble();
		case LogicType.RatioOxygen:
		case LogicType.RatioCarbonDioxide:
		case LogicType.RatioNitrogen:
		case LogicType.RatioPollutant:
		case LogicType.RatioMethane:
		case LogicType.RatioWater:
		case LogicType.RatioNitrousOxide:
		case LogicType.RatioLiquidNitrogen:
		case LogicType.RatioLiquidOxygen:
		case LogicType.RatioLiquidMethane:
		case LogicType.RatioSteam:
		case LogicType.RatioLiquidCarbonDioxide:
		case LogicType.RatioLiquidPollutant:
		case LogicType.RatioLiquidNitrousOxide:
		case LogicType.RatioHydrogen:
		case LogicType.RatioLiquidHydrogen:
		case LogicType.RatioPollutedWater:
		case LogicType.RatioHydrazine:
		case LogicType.RatioLiquidHydrazine:
		case LogicType.RatioLiquidAlcohol:
		case LogicType.RatioHelium:
		case LogicType.RatioLiquidSodiumChloride:
		case LogicType.RatioSilanol:
		case LogicType.RatioLiquidSilanol:
		case LogicType.RatioHydrochloricAcid:
		case LogicType.RatioLiquidHydrochloricAcid:
		case LogicType.RatioOzone:
		case LogicType.RatioLiquidOzone:
			return GasRatio(logicType);
		case LogicType.VolumeOfLiquid:
			return VolumeOfLiquid.ToDouble();
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.AtmosDevices);
	}

	public override double GasRatio(LogicType logicType)
	{
		FindAtmosphere();
		if (WorldAtmosphere == null)
		{
			return 0.0;
		}
		return AtmosphereHelper.GasRatio(logicType, WorldAtmosphere);
	}

	public void FindAtmosphere()
	{
		if (GameManager.IsMainThread)
		{
			if (_lastFrameCheck == Time.frameCount)
			{
				return;
			}
			_lastFrameCheck = Time.frameCount;
		}
		WorldAtmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
