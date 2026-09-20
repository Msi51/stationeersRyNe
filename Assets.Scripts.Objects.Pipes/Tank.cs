using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class Tank : DeviceInternal, IRocketInternals, IRocketComponent
{
	[SerializeField]
	private RocketInternalCellType _rocketInternalCellType;

	public RocketInternalCellType InternalCellType => _rocketInternalCellType;

	public bool StrictlyInternal => InternalCellType != RocketInternalCellType.None;

	public RocketNetwork RocketNetwork { get; set; }

	public override AtmosphereHelper.MatterState MatterState => AtmosphereHelper.MatterState.All;

	public override float ConvectionFactor => ThermodynamicsScale * 0.5f;

	public override float RadiationFactor => ThermodynamicsScale * 0.02f;

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(20f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		if (base.HasOpenGrid && base.InternalAtmosphere != null && IsBroken)
		{
			atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			AtmosphereHelper.Mix(base.InternalAtmosphere, atmosphere, MatterState);
			return;
		}
		PressurekPa pressurekPa = PressurekPa.Zero;
		PressurekPa pressurekPa2 = PressurekPa.Zero;
		if (base.InternalAtmosphere != null)
		{
			pressurekPa2 = base.InternalAtmosphere.PressureGassesAndLiquids;
			if (atmosphere != null)
			{
				pressurekPa = atmosphere.PressureGassesAndLiquids;
			}
		}
		PressurekPa pressurekPa3 = RocketMath.Abs(pressurekPa - pressurekPa2);
		PressurekPa pressurekPa4 = ((ContentType == Pipe.ContentType.Gas) ? Chemistry.Limits.MAXPressureGasPipe : Chemistry.Limits.MAXPressureLiquidPipe);
		if (pressurekPa3 >= pressurekPa4)
		{
			DamageState.Damage(ChangeDamageType.Increment, 5f, DamageUpdateType.Brute);
		}
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			if (GameManager.GameState != GameState.None && !IsCursor)
			{
				AtmosphericEventInstance.CloneGlobalAddGasMix(base.WorldGrid, base.InternalAtmosphere.GasMixture, base.InternalAtmosphere.Inflamed);
				AtmosphericEventInstance.Reset(base.InternalAtmosphere);
			}
			base.OnDestroy();
		}
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
		case LogicType.Volume:
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
			if (!base.InternalAtmosphere.Sparked)
			{
				return 0.0;
			}
			return 1.0;
		case LogicType.Pressure:
			return base.InternalAtmosphere.PressureGassesAndLiquids.ToDouble();
		case LogicType.Quantity:
		case LogicType.TotalMoles:
			return base.InternalAtmosphere.TotalMoles.ToDouble();
		case LogicType.Temperature:
			return base.InternalAtmosphere.Temperature.ToDouble();
		case LogicType.Volume:
			return base.InternalAtmosphere.Volume.ToDouble();
		case LogicType.VolumeOfLiquid:
			return base.InternalAtmosphere.TotalVolumeLiquids.ToDouble();
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
		default:
			return base.GetLogicValue(logicType);
		}
	}
}
