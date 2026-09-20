using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using UnityEngine;
using Weather;

namespace Assets.Scripts;

public static class PlanetaryAtmosphereSimulation
{
	public static MoleEnergy LatentEnergyOffset;

	public static MoleEnergy ExternalInputEnergyOffset;

	private const double CLOUD_VOLUME = 100000.0;

	private const double ICE_CAP_VOLUME = 10000000.0;

	private static GlobalGasMix _liquidClouds;

	private static GlobalGasMix _iceClouds;

	private static GlobalGasMix _iceCaps;

	private static GlobalGasMix _globalGasMix;

	public static float SpaceHeight = 1000f;

	private static float _spaceGridHeight = SpaceHeight * 10f;

	public const double MELT_RATIO = 1.388888888888889E-05;

	public const double FREEZE_RATIO = 4.1666666666666665E-05;

	public static MoleQuantity MINMeltQuantity = new MoleQuantity(1000.0);

	public static MoleQuantity MINLiquidFreezeQuantity = new MoleQuantity(3000.0);

	private static readonly object GlobalInteraction = new Object();

	private static Atmosphere _readOnlyGlobal;

	private static Atmosphere _readOnlySpace;

	private const int UPDATE_TO_VULCAN_VENUS_ATMO = 26973;

	public static TemperatureKelvin AggregateTemperature;

	public static TemperatureKelvin GhgIndexOffset;

	public static TemperatureKelvin SolarAngleTemperature;

	public static TemperatureKelvin SolarDistanceOffsetTemperature;

	public static TemperatureKelvin DensityOffsetTemperature;

	public static TemperatureKelvin WeatherOffset;

	public static TemperatureKelvin LatentOffset;

	public static TemperatureKelvin ExternalInputOffset;

	public static bool IsGlobalInteraction => false;

	public static VolumeLitres GlobalVolume => _globalGasMix.Volume;

	public static PressurekPa GlobalPressure => _readOnlyGlobal?.PressureGasses ?? PressurekPa.Zero;

	public static bool DrawGlobalDebug { get; set; }

	public static float LiquidVolumeRatio { get; private set; }

	public static VolumeLitres LiquidVolume { get; private set; }

	public static bool IsInSpaceAtmosphere(WorldGrid wGrid)
	{
		return IsInSpaceAtmosphere(wGrid.Value);
	}

	public static bool IsInSpaceAtmosphere(Grid3 grid3)
	{
		return (float)grid3.y >= _spaceGridHeight;
	}

	public static Atmosphere ReadOnlyGlobal(WorldGrid wGrid)
	{
		if (IsInSpaceAtmosphere(wGrid))
		{
			return _readOnlySpace;
		}
		return _readOnlyGlobal;
	}

	public static void CloneGlobalGasMix(Atmosphere targetAtmosphere)
	{
		if ((float)targetAtmosphere.WorldGrid.Value.y >= _spaceGridHeight)
		{
			return;
		}
		double ratio = (targetAtmosphere.Volume / _globalGasMix.Volume).ToDouble();
		GasMixture gasMixture = _globalGasMix.ToInstancedGasMixture();
		gasMixture.Scale(ratio);
		targetAtmosphere.GasMixture.Set(gasMixture);
		if (!IsGlobalInteraction)
		{
			return;
		}
		lock (GlobalInteraction)
		{
			_globalGasMix.Remove(gasMixture);
		}
	}

	public static MoleQuantity GetGlobalMoles(AtmosphereHelper.MatterState matterState)
	{
		if (IsGlobalInteraction)
		{
			lock (GlobalInteraction)
			{
				return _globalGasMix.TotalQuantity(matterState) * (Chemistry.GridVolume / GlobalVolume).ToDouble();
			}
		}
		return _readOnlyGlobal.GasMixture.GetTotalMoles(matterState);
	}

	public static GasMixture TakeGlobalGasMix(VolumeLitres takeVolume)
	{
		double ratio = (takeVolume / _globalGasMix.Volume).ToDouble();
		GasMixture gasMixture = _globalGasMix.ToInstancedGasMixture();
		gasMixture.Scale(ratio);
		if (IsGlobalInteraction)
		{
			lock (GlobalInteraction)
			{
				_globalGasMix.Remove(gasMixture);
			}
		}
		return gasMixture;
	}

	public static GasMixture TakeGlobalMoles(MoleQuantity quantity, AtmosphereHelper.MatterState matterState)
	{
		quantity = RocketMath.Min(quantity, _globalGasMix.TotalQuantityGas());
		if (IsGlobalInteraction)
		{
			lock (GlobalInteraction)
			{
				return _globalGasMix.Remove(quantity, matterState);
			}
		}
		return GasMixtureHelper.Create(_readOnlyGlobal.GasMixture).Remove(quantity, matterState);
	}

	public static void GiveToGlobal(GasMixture gasMixture)
	{
		if (!IsGlobalInteraction)
		{
			return;
		}
		lock (GlobalInteraction)
		{
			_globalGasMix.Add(gasMixture);
			TemperatureKelvin temperatureKelvin = gasMixture.Temperature - _globalGasMix.GetGlobalGasMixTemperature(WorldSetting.Current.Data.GlobalAtmosphereData);
			ExternalInputEnergyOffset += IdealGas.Energy(gasMixture.HeatCapacity, temperatureKelvin);
		}
	}

	public static void AddEnergy(MoleEnergy energy)
	{
		if (!IsGlobalInteraction || energy <= MoleEnergy.Zero)
		{
			return;
		}
		lock (GlobalInteraction)
		{
			ExternalInputEnergyOffset += energy;
		}
	}

	public static void RemoveEnergy(MoleEnergy energy)
	{
		if (!IsGlobalInteraction || energy <= MoleEnergy.Zero)
		{
			return;
		}
		lock (GlobalInteraction)
		{
			ExternalInputEnergyOffset -= energy;
		}
	}

	public static GasMixture GetGlobalGasMixCopy(AtmosphereHelper.MatterState matterState)
	{
		return GasMixtureHelper.Create(_globalGasMix, matterState);
	}

	public static void CreateGlobalAtmosphere(GlobalAtmosphereData data)
	{
		_globalGasMix = GlobalGasMix.Create(data);
		_liquidClouds = new GlobalGasMix(new VolumeLitres(100000.0))
		{
			DisplayName = "Liquid Clouds"
		};
		_iceClouds = new GlobalGasMix(new VolumeLitres(100000.0))
		{
			DisplayName = "Ice Clouds"
		};
		_iceCaps = new GlobalGasMix(new VolumeLitres(10000000.0))
		{
			DisplayName = "Ice Caps"
		};
		_readOnlySpace = new Atmosphere
		{
			Volume = Chemistry.GridVolume,
			GasMixture = GasMixtureHelper.Create(),
			Mode = AtmosphereHelper.AtmosphereMode.Global
		};
		_readOnlyGlobal = new Atmosphere
		{
			Volume = Chemistry.GridVolume,
			Mode = AtmosphereHelper.AtmosphereMode.Global
		};
		GasMixture gasMixture = _globalGasMix.ToInstancedGasMixture();
		gasMixture.Scale((Chemistry.GridVolume / _globalGasMix.VolumeForGas()).ToDouble());
		_readOnlyGlobal.GasMixture.Set(gasMixture);
		_readOnlyGlobal.GasMixture.SetReadOnly(isReadOnly: true);
		_readOnlySpace.GasMixture.SetReadOnly(isReadOnly: true);
	}

	public static PlanetaryAtmosphereSaveData Save()
	{
		return new PlanetaryAtmosphereSaveData
		{
			GlobalGasMix = GlobalGasMixSaveData.Create(_globalGasMix),
			LiquidClouds = GlobalGasMixSaveData.Create(_liquidClouds),
			IceClouds = GlobalGasMixSaveData.Create(_iceClouds),
			IceCaps = GlobalGasMixSaveData.Create(_iceCaps),
			LatentOffset = new DoubleReference(LatentEnergyOffset.ToDouble()),
			ExternalOffset = new DoubleReference(ExternalInputEnergyOffset.ToDouble())
		};
	}

	private static bool ResetOnSaveVersion(int saveVersion)
	{
		if (saveVersion < 26973)
		{
			return true;
		}
		return false;
	}

	public static void Load(PlanetaryAtmosphereSaveData saveData, int saveVersion)
	{
		if (!ResetOnSaveVersion(saveVersion) && saveData != null)
		{
			if (saveData.GlobalGasMix != null)
			{
				_globalGasMix.Load(saveData.GlobalGasMix);
			}
			if (saveData.IceCaps != null)
			{
				_iceCaps.Load(saveData.IceCaps);
			}
			if (saveData.LiquidClouds != null)
			{
				_liquidClouds.Load(saveData.LiquidClouds);
			}
			if (saveData.IceClouds != null)
			{
				_iceClouds.Load(saveData.IceClouds);
			}
			if (saveData.LatentOffset != null)
			{
				LatentEnergyOffset = new MoleEnergy(saveData.LatentOffset.Value);
			}
			if (saveData.ExternalOffset != null)
			{
				ExternalInputEnergyOffset = new MoleEnergy(saveData.ExternalOffset.Value);
			}
		}
	}

	public static void RegenerateGlobalFromData(GlobalAtmosphereData data)
	{
		_globalGasMix = GlobalGasMix.Create(data);
		GasMixture gasMixture = _globalGasMix.ToInstancedGasMixture();
		gasMixture.Scale((Chemistry.GridVolume / _globalGasMix.VolumeForGas()).ToDouble());
		_readOnlyGlobal.GasMixture.Set(gasMixture);
		_readOnlyGlobal.GasMixture.SetReadOnly(isReadOnly: true);
		_readOnlySpace.GasMixture.SetReadOnly(isReadOnly: true);
	}

	public static HeatCapacity GetHeatCapacity()
	{
		return _globalGasMix.GetHeatCapacity() + _liquidClouds.GetHeatCapacity() + _iceClouds.GetHeatCapacity() + _iceCaps.GetHeatCapacity();
	}

	public static TemperatureKelvin GetLatentTemperatureOffset()
	{
		if (GameManager.GameState == GameState.None)
		{
			return TemperatureKelvin.Zero;
		}
		return IdealGas.Temperature(LatentEnergyOffset, GetHeatCapacity());
	}

	public static TemperatureKelvin GetExternalInputEnergyOffset()
	{
		if (GameManager.GameState == GameState.None)
		{
			return TemperatureKelvin.Zero;
		}
		return IdealGas.Temperature(ExternalInputEnergyOffset, GetHeatCapacity());
	}

	public static void Draw()
	{
		_globalGasMix?.Draw();
		_iceCaps?.Draw();
		_liquidClouds?.Draw();
		_iceClouds?.Draw();
	}

	public static void DrawOnScreenDebug()
	{
		if (DrawGlobalDebug)
		{
			_globalGasMix?.DrawDebugInfo();
		}
	}

	public static void TickPlanetarySimulation()
	{
		HandleGlobalStateChange();
		if (_liquidClouds.VolumeOfLiquid() >= _liquidClouds.Volume)
		{
			if (WeatherManager.WeatherState != WeatherState.Storm && WeatherManager.WeatherState != WeatherState.Rain && WeatherManager.WeatherState != WeatherState.RainScheduled)
			{
				_globalGasMix.Add(_liquidClouds, AtmosphereHelper.MatterState.All);
				_liquidClouds.ClearQuantities(AtmosphereHelper.MatterState.All);
				WeatherManager.ScheduleWeatherEvent(DataCollection.Get<WeatherEvent>(WeatherManager.RainWeatherEvent));
			}
			else if (WeatherManager.WeatherState == WeatherState.Snow)
			{
				WeatherManager.StopCurrentWeatherEvent();
				WeatherManager.ImmediatelyActivateWeatherEvent(DataCollection.Get<WeatherEvent>(WeatherManager.RainWeatherEvent));
			}
		}
		if (_iceClouds.VolumeOfLiquid() >= _iceClouds.Volume)
		{
			if (WeatherManager.WeatherState == WeatherState.Rain)
			{
				WeatherManager.StopCurrentWeatherEvent();
				WeatherManager.ImmediatelyActivateWeatherEvent(DataCollection.Get<WeatherEvent>(WeatherManager.SnowWeatherEvent));
			}
			else if (WeatherManager.WeatherState != WeatherState.Storm && WeatherManager.WeatherState != WeatherState.Snow && WeatherManager.WeatherState != WeatherState.SnowScheduled)
			{
				_globalGasMix.Add(_iceClouds, AtmosphereHelper.MatterState.All);
				_iceClouds.ClearQuantities(AtmosphereHelper.MatterState.All);
				WeatherManager.ScheduleWeatherEvent(DataCollection.Get<WeatherEvent>(WeatherManager.SnowWeatherEvent));
			}
		}
		GasMixture gasMixture = _globalGasMix.ToInstancedGasMixture();
		gasMixture.Scale((Chemistry.GridVolume / _globalGasMix.Volume).ToDouble());
		_readOnlyGlobal.GasMixture.SetReadOnly(isReadOnly: false);
		_readOnlyGlobal.GasMixture.Set(gasMixture);
		_readOnlyGlobal.GasMixture.SetReadOnly(isReadOnly: true);
		CacheTemperatureCurveOffsets();
		CacheValues();
	}

	private static void CacheValues()
	{
		LiquidVolumeRatio = (_globalGasMix.VolumeOfLiquid() / GlobalVolume).ToFloat();
		LiquidVolume = _globalGasMix.VolumeOfLiquid();
	}

	private static void ClearCachedValues()
	{
		AggregateTemperature = TemperatureKelvin.Zero;
		GhgIndexOffset = TemperatureKelvin.Zero;
		SolarAngleTemperature = TemperatureKelvin.Zero;
		SolarDistanceOffsetTemperature = TemperatureKelvin.Zero;
		DensityOffsetTemperature = TemperatureKelvin.Zero;
		WeatherOffset = TemperatureKelvin.Zero;
		LatentOffset = TemperatureKelvin.Zero;
		ExternalInputOffset = TemperatureKelvin.Zero;
	}

	public static void CacheTemperatureCurveOffsets()
	{
		GlobalAtmosphereData globalAtmosphereData = WorldSetting.Current.Data.GlobalAtmosphereData;
		float num = Vector3.Angle(Vector3.up, OrbitalSimulation.WorldSunVector);
		float solarEnergyPercentClamped = OrbitalSimulation.System.GetSolarEnergyPercentClamped(OrbitalSimulation.System.GetSolarEnergy(), OrbitalSimulation.System.CalculateSolarIrradiance());
		float ghgIndex = TerraForming.GetGhgIndex(_globalGasMix);
		double milliMolesPerLitre = IdealGas.GetMilliMolesPerLitre(_globalGasMix.Volume, _globalGasMix.TotalQuantityGas());
		SolarAngleTemperature = globalAtmosphereData.GetSolarAngleTemperature(num);
		TemperatureKelvin solarAngleTemperature = SolarAngleTemperature;
		if (WeatherManager.IsWeatherEventRunning && WeatherManager.CurrentWeatherEvent != null)
		{
			WeatherOffset = new TemperatureKelvin(WeatherManager.CurrentWeatherEvent.TemperatureOffset.GetOffset(num));
			solarAngleTemperature += WeatherOffset;
		}
		SolarDistanceOffsetTemperature = globalAtmosphereData.GetSolarDistanceTemperatureOffset(num, solarEnergyPercentClamped);
		GhgIndexOffset = globalAtmosphereData.GetGHGTemperatureOffset(num, ghgIndex);
		DensityOffsetTemperature = globalAtmosphereData.GetDensityOffset(num, milliMolesPerLitre);
		LatentOffset = GetLatentTemperatureOffset();
		ExternalInputOffset = GetExternalInputEnergyOffset();
		if (!SolarDistanceOffsetTemperature.IsNaN())
		{
			solarAngleTemperature += SolarDistanceOffsetTemperature;
		}
		if (!GhgIndexOffset.IsNaN())
		{
			solarAngleTemperature += GhgIndexOffset;
		}
		if (!DensityOffsetTemperature.IsNaN())
		{
			solarAngleTemperature += DensityOffsetTemperature;
		}
		if (!LatentOffset.IsNaN())
		{
			solarAngleTemperature += LatentOffset;
		}
		if (!ExternalInputOffset.IsNaN())
		{
			solarAngleTemperature += ExternalInputOffset;
		}
		AggregateTemperature = solarAngleTemperature;
	}

	public static void HandleGlobalStateChange()
	{
		MeltIceCapsToGlobal();
		FreezeGlobalLiquidToIceCaps();
		EvaporateGlobalLiquid();
		CondenseGlobalGasToLiquidClouds();
		FreezeGlobalGasToIceClouds();
	}

	private static void FreezeGlobalGasToIceClouds()
	{
		Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
		foreach (Chemistry.GasType gasType in values)
		{
			if (Mole.MatterState(gasType) == AtmosphereHelper.MatterState.Gas)
			{
				LatentEnergyOffset += _globalGasMix.FreezeMoleQuantity(gasType, _iceClouds, MoleQuantity.MaxValue);
			}
		}
	}

	private static void CondenseGlobalGasToLiquidClouds()
	{
		Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
		foreach (Chemistry.GasType gasType in values)
		{
			if (Mole.MatterState(gasType) == AtmosphereHelper.MatterState.Gas)
			{
				LatentEnergyOffset += _globalGasMix.StateChangeMoleQuantity(gasType, _liquidClouds);
			}
		}
	}

	private static void MeltIceCapsToGlobal()
	{
		Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
		foreach (Chemistry.GasType gasType in values)
		{
			if (Mole.MatterState(gasType) == AtmosphereHelper.MatterState.Liquid)
			{
				LatentEnergyOffset += _iceCaps.MeltMoleQuantity(gasType, _globalGasMix, RocketMath.Max(MINMeltQuantity, _iceCaps.Get(gasType) * 1.388888888888889E-05));
			}
		}
	}

	private static void FreezeGlobalLiquidToIceCaps()
	{
		Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
		foreach (Chemistry.GasType gasType in values)
		{
			if (Mole.MatterState(gasType) == AtmosphereHelper.MatterState.Liquid)
			{
				LatentEnergyOffset += _globalGasMix.FreezeMoleQuantity(gasType, _iceCaps, RocketMath.Max(MINLiquidFreezeQuantity, _globalGasMix.Get(gasType) * 4.1666666666666665E-05));
			}
		}
	}

	private static void EvaporateGlobalLiquid()
	{
		Chemistry.GasType[] values = EnumCollections.GasTypes.Values;
		foreach (Chemistry.GasType gasType in values)
		{
			if (Mole.MatterState(gasType) == AtmosphereHelper.MatterState.Liquid)
			{
				LatentEnergyOffset += _globalGasMix.TryEvaporateGroundLiquid(gasType, _globalGasMix);
			}
		}
	}

	public static TemperatureKelvin GetGlobalAverageTemperature()
	{
		return TemperatureKelvin.Zero;
	}

	public static GlobalGasMix GetGlobalGasMix()
	{
		return _globalGasMix;
	}

	public static void Clear()
	{
		_globalGasMix = null;
		_iceCaps = null;
		_iceClouds = null;
		_liquidClouds = null;
		LatentEnergyOffset = MoleEnergy.Zero;
		ExternalInputEnergyOffset = MoleEnergy.Zero;
		ClearCachedValues();
	}

	public static double LiquidVolumeMultiplier()
	{
		if (_globalGasMix == null)
		{
			return 1.0;
		}
		return GasMixtureHelper.Create(_globalGasMix, AtmosphereHelper.MatterState.All).InWorldLiquidVolumeMultiplier();
	}
}
