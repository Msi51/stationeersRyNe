using System;
using System.Xml.Serialization;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Networks;
using TerrainSystem;
using UnityEngine;

namespace Assets.Scripts.Atmospherics;

public static class AtmosphereHelper
{
	public enum AtmosphereMode : byte
	{
		[XmlEnum("World")]
		World,
		[XmlEnum("Network")]
		Network,
		[XmlEnum("Thing")]
		Thing,
		[XmlEnum("Global")]
		Global,
		[XmlEnum("None")]
		None
	}

	public enum MatterState : byte
	{
		Liquid,
		Gas,
		All,
		None
	}

	public const double FLOW_RATE_LIMITER = 4.0;

	public static float MixingRatioRoom = 0.3f;

	private static readonly float BaseGlobalAtmosphereNeighbourThreshold = 100f;

	public static float GlobalAtmosphereNeighbourThreshold = BaseGlobalAtmosphereNeighbourThreshold;

	private static readonly float DampFactor = 10f;

	private const float BASE_LERP_RATE = 0.2f;

	public static System.Random _random = new System.Random();

	public static readonly MoleQuantity MinimumMolesForProcessing = new MoleQuantity(0.0003);

	public const double HEAT_TRANSFER_COEFFICIENT = 100.0;

	public static bool CanWriteAccess => ThreadedManager.IsThread;

	public static double GasRatio(LogicType logicType, Atmosphere atmosphere)
	{
		if (atmosphere == null)
		{
			return 0.0;
		}
		return GasMixtureHelper.GasRatio(logicType, atmosphere.GasMixture);
	}

	public static void Mix(Atmosphere inputAtmos, Atmosphere outputAtmos, MatterState matterState)
	{
		if (inputAtmos != null && outputAtmos != null)
		{
			GasMixture gasMixture = GasMixtureHelper.Create();
			VolumeLitres zero = VolumeLitres.Zero;
			gasMixture.Add(inputAtmos.GasMixture, matterState);
			zero += inputAtmos.GetVolume(matterState);
			gasMixture.Add(outputAtmos.GasMixture, matterState);
			zero += outputAtmos.GetVolume(matterState);
			GasMixture gasMixture2 = new GasMixture(gasMixture);
			gasMixture2.Scale((inputAtmos.GetVolume(matterState) / zero).ToFloat(), matterState);
			inputAtmos.GasMixture.Set(gasMixture2, matterState);
			GasMixture gasMixture3 = new GasMixture(gasMixture);
			gasMixture3.Scale((outputAtmos.GetVolume(matterState) / zero).ToFloat(), matterState);
			outputAtmos.GasMixture.Set(gasMixture3, matterState);
		}
	}

	public static void MoveToEqualize(Atmosphere inputAtmos, Atmosphere outputAtmos, PressurekPa desiredPressureChange, MatterState typeToMove)
	{
		if (inputAtmos != null && outputAtmos != null)
		{
			switch (typeToMove)
			{
			case MatterState.Liquid:
				outputAtmos.Add(RemoveLiquidToEqualize(inputAtmos, outputAtmos));
				break;
			case MatterState.Gas:
				outputAtmos.Add(RemoveGasToEqualize(inputAtmos, outputAtmos, desiredPressureChange));
				break;
			case MatterState.All:
				outputAtmos.Add(RemoveLiquidToEqualize(inputAtmos, outputAtmos));
				outputAtmos.Add(RemoveGasToEqualize(inputAtmos, outputAtmos, desiredPressureChange));
				break;
			}
		}
	}

	public static bool MoveRegulatedLiquidVolume(Atmosphere input, Atmosphere output, VolumeLitres maxVolumePerTick, float setting, RegulatorType regulatorType)
	{
		if (input.LiquidVolumeRatio > output.LiquidVolumeRatio)
		{
			VolumeLitres volumeLitres = input.Volume + output.Volume;
			VolumeLitres volumeLitres2 = (input.TotalVolumeLiquids + output.TotalVolumeLiquids) / volumeLitres;
			VolumeLitres val = (output.Volume * volumeLitres2 - output.TotalVolumeLiquids) / 4.0;
			maxVolumePerTick = RocketMath.Max(maxVolumePerTick, val);
		}
		MoleQuantity val2 = ((!(input.GasMixture.GetMolarVolumeLiquids() <= VolumeLitres.Zero)) ? new MoleQuantity((maxVolumePerTick / input.GasMixture.GetMolarVolumeLiquids()).ToDouble()) : MoleQuantity.Zero);
		double num = 0.0;
		double num2 = 0.0;
		double num3 = (double)setting / 100.0;
		MoleQuantity moleQuantity = MoleQuantity.Zero;
		switch (regulatorType)
		{
		case RegulatorType.Upstream:
		{
			num = output.LiquidVolumeRatio;
			if (num >= num3)
			{
				return false;
			}
			num2 = num3 - num;
			VolumeLitres volumeLitres3 = output.Volume * num2;
			moleQuantity = ((input.GasMixture.GetMolarVolumeLiquids() <= VolumeLitres.Zero) ? MoleQuantity.Zero : new MoleQuantity((volumeLitres3 / input.GasMixture.GetMolarVolumeLiquids()).ToDouble()));
			moleQuantity = RocketMath.Min(moleQuantity, val2);
			break;
		}
		case RegulatorType.Downstream:
		{
			num = input.LiquidVolumeRatio;
			if (num <= num3)
			{
				return false;
			}
			num2 = num - num3;
			VolumeLitres volumeLitres3 = input.Volume * num2;
			moleQuantity = ((input.GasMixture.GetMolarVolumeLiquids() <= VolumeLitres.Zero) ? MoleQuantity.Zero : new MoleQuantity((volumeLitres3 / input.GasMixture.GetMolarVolumeLiquids()).ToDouble()));
			moleQuantity = RocketMath.Min(moleQuantity, val2);
			break;
		}
		}
		if (moleQuantity <= MoleQuantity.Zero)
		{
			return false;
		}
		output.Add(input.Remove(moleQuantity, MatterState.Liquid));
		return true;
	}

	public static void MoveRegulatedGas(Atmosphere input, Atmosphere output, PressurekPa pressurePerTick, float setting, RegulatorType regulatorType, MatterState movedContent)
	{
		MoleQuantity val = IdealGas.Quantity(pressurePerTick, Chemistry.PipeVolume, input.Temperature);
		if (input.PressureGasses > output.PressureGasses)
		{
			val = RocketMath.Max(MaxMolesPerTick(input, output), val);
		}
		MoleQuantity moleQuantity = MoleQuantity.Zero;
		switch (regulatorType)
		{
		case RegulatorType.Upstream:
		{
			PressurekPa pressureGassesAndLiquids = output.PressureGassesAndLiquids;
			if (pressureGassesAndLiquids.ToDouble() >= (double)setting)
			{
				return;
			}
			moleQuantity = RocketMath.Min(IdealGas.Quantity(new PressurekPa((double)setting - pressureGassesAndLiquids.ToDouble()), output.GetVolume(MatterState.Gas), input.Temperature), val);
			break;
		}
		case RegulatorType.Downstream:
		{
			PressurekPa pressureGassesAndLiquids = input.PressureGassesAndLiquids;
			if (pressureGassesAndLiquids.ToDouble() <= (double)setting)
			{
				return;
			}
			moleQuantity = RocketMath.Min(IdealGas.Quantity(new PressurekPa(pressureGassesAndLiquids.ToDouble() - (double)setting), input.GetVolume(MatterState.Gas), input.Temperature), val);
			break;
		}
		}
		if (!(moleQuantity <= MoleQuantity.Zero))
		{
			GasMixture gasMixture = input.Remove(moleQuantity, movedContent);
			output.Add(gasMixture);
		}
	}

	public static GasMixture TakeNormalisedGasPressureScaled(Atmosphere inputAtmosphere, PressurekPa basePressurePerTick, PressurekPa inputPressureDelta, out MoleQuantity transferMoles, MatterState matterState = MatterState.All, float denominator = 3f)
	{
		PressurekPa outMax = Chemistry.Limits.MAXPressureGasPipe / denominator;
		inputPressureDelta = RocketMath.Max(inputPressureDelta, PressurekPa.Zero);
		PressurekPa pressure = RocketMath.MapToScale(PressurekPa.Zero, Chemistry.Limits.MAXPressureGasPipe, basePressurePerTick, outMax, inputPressureDelta);
		transferMoles = IdealGas.Quantity(pressure, Chemistry.PipeVolume, inputAtmosphere.Temperature);
		return inputAtmosphere.Remove(transferMoles, matterState);
	}

	public static GasMixture TakeNormalisedLiquidVolumeScaled(Atmosphere inputAtmosphere, VolumeLitres baseVolumePerTick, float outputLiquidVolumeRatio, out MoleQuantity transferMoles)
	{
		VolumeLitres liquidPipeVolume = Chemistry.LiquidPipeVolume;
		VolumeLitres volume = RocketMath.MapToScale(value: new VolumeLitres(Mathf.Max(0f, inputAtmosphere.LiquidVolumeRatio - outputLiquidVolumeRatio)), min: VolumeLitres.Zero, max: VolumeLitres.One, outMin: baseVolumePerTick, outMax: liquidPipeVolume);
		transferMoles = IdealGas.Quantity(volume, inputAtmosphere.GasMixture.GetMolarVolumeLiquids());
		return inputAtmosphere.Remove(transferMoles, MatterState.Liquid);
	}

	private static MoleQuantity MaxMolesPerTick(Atmosphere input, Atmosphere output)
	{
		PressurekPa pressurekPa = input.PressureGasses - output.PressureGasses;
		PressurekPa pressurekPa2 = IdealGas.PressurePerMole(input.Temperature, input.GetVolume(MatterState.Gas));
		PressurekPa pressurekPa3 = IdealGas.PressurePerMole(input.Temperature, output.GetVolume(MatterState.Gas));
		PressurekPa pressurekPa4 = pressurekPa2 + pressurekPa3;
		return new MoleQuantity(pressurekPa.ToDouble() / pressurekPa4.ToDouble() / 4.0);
	}

	public static GasMixture RemoveToEqualise(Atmosphere inputAtmos, Atmosphere outputAtmos, PressurekPa desiredPressureChange, MatterState matterState = MatterState.All)
	{
		switch (matterState)
		{
		case MatterState.Liquid:
			return RemoveLiquidToEqualize(inputAtmos, outputAtmos);
		case MatterState.Gas:
			return RemoveGasToEqualize(inputAtmos, outputAtmos, desiredPressureChange);
		case MatterState.All:
		{
			GasMixture result = RemoveLiquidToEqualize(inputAtmos, outputAtmos);
			if (result.IsValid)
			{
				result.Add(RemoveGasToEqualize(inputAtmos, outputAtmos, desiredPressureChange));
				return result;
			}
			return RemoveGasToEqualize(inputAtmos, outputAtmos, desiredPressureChange);
		}
		default:
			return GasMixtureHelper.Invalid;
		}
	}

	private static GasMixture RemoveLiquidToEqualize(Atmosphere inputAtmos, Atmosphere outputAtmos)
	{
		if (inputAtmos.LiquidVolumeRatio > outputAtmos.LiquidVolumeRatio)
		{
			VolumeLitres volumeLitres = inputAtmos.Volume + outputAtmos.Volume;
			double num = ((inputAtmos.TotalVolumeLiquids + outputAtmos.TotalVolumeLiquids) / volumeLitres).ToDouble() - (double)outputAtmos.LiquidVolumeRatio;
			VolumeLitres volumeLitres2 = outputAtmos.Volume * num;
			VolumeLitres molarVolumeLiquids = inputAtmos.GasMixture.GetMolarVolumeLiquids();
			MoleQuantity transferMoles = ((!(molarVolumeLiquids <= VolumeLitres.Zero)) ? new MoleQuantity((volumeLitres2 / molarVolumeLiquids).ToDouble()) : MoleQuantity.Zero);
			return inputAtmos.Remove(transferMoles, MatterState.Liquid);
		}
		return GasMixtureHelper.Invalid;
	}

	private static GasMixture RemoveGasToEqualize(Atmosphere inputAtmos, Atmosphere outputAtmos, PressurekPa desiredPressureChange)
	{
		PressurekPa pressurekPa = RocketMath.Min(desiredPressureChange, inputAtmos.PressureGassesAndLiquids - outputAtmos.PressureGassesAndLiquids);
		if (pressurekPa > PressurekPa.Zero)
		{
			double num = 8.3144 * inputAtmos.Temperature.ToDouble() / inputAtmos.GetVolume(MatterState.Gas).ToDouble();
			double num2 = 8.3144 * inputAtmos.Temperature.ToDouble() / outputAtmos.GetVolume(MatterState.Gas).ToDouble();
			double num3 = num + num2;
			MoleQuantity moleQuantity = new MoleQuantity(pressurekPa.ToDouble() / num3);
			if (moleQuantity <= MoleQuantity.Zero)
			{
				return GasMixtureHelper.Invalid;
			}
			return inputAtmos.Remove(moleQuantity, MatterState.Gas);
		}
		return GasMixtureHelper.Invalid;
	}

	public static void MoveToEqualizeBidirectional(Atmosphere inputAtmos, Atmosphere outputAtmos, PressurekPa amountPressureToMove, MatterState typeToMove, MoleQuantity maxMoles)
	{
		Atmosphere atmosphere = inputAtmos;
		Atmosphere atmosphere2 = outputAtmos;
		if (outputAtmos.PressureGassesAndLiquids > inputAtmos.PressureGassesAndLiquids)
		{
			atmosphere = outputAtmos;
			atmosphere2 = inputAtmos;
		}
		PressurekPa pressurekPa = RocketMath.Min(amountPressureToMove, atmosphere.PressureGassesAndLiquids - atmosphere2.PressureGassesAndLiquids);
		if (pressurekPa > PressurekPa.Zero)
		{
			MoleQuantity val = IdealGas.Quantity(pressurekPa, RocketMath.Min(atmosphere.GetVolume(typeToMove), atmosphere2.GetVolume(typeToMove)), atmosphere.Temperature);
			val = RocketMath.Min(val, maxMoles);
			GasMixture gasMixture = atmosphere.Remove(val, typeToMove);
			atmosphere2.Add(gasMixture);
		}
	}

	public static bool FilterGas(Chemistry.GasType filterType, ref GasMixture fromMix, ref GasMixture tooMix)
	{
		Mole mole = fromMix.RemoveAll(filterType);
		Mole mole2 = fromMix.RemoveAll(MoleHelper.CondensationType(filterType));
		bool result = mole.Quantity + mole2.Quantity > MoleQuantity.Zero;
		tooMix.Add(mole);
		tooMix.Add(mole2);
		return result;
	}

	public static MoleQuantity FilterGas(Chemistry.GasType filterType, ref GasMixture fromMix, ref GasMixture tooMix, Atmosphere atmosphere, double minRatio)
	{
		Mole mole;
		Mole mole2;
		if (Mole.MatterState(filterType) == MatterState.Liquid)
		{
			mole = fromMix.RemoveAll(filterType);
			mole2 = MoleHelper.Invalid;
		}
		else
		{
			mole2 = fromMix.RemoveAll(filterType);
			mole = fromMix.RemoveAll(MoleHelper.CondensationType(filterType));
		}
		MoleQuantity result = mole2.Quantity + mole.Quantity;
		tooMix.Add(mole2);
		tooMix.Add(mole);
		if ((atmosphere.GasMixture.GetMoleValue(filterType).Quantity / atmosphere.TotalMoles).ToDouble() < minRatio)
		{
			tooMix.Add(atmosphere.GasMixture.RemoveAll(filterType));
		}
		if ((atmosphere.GasMixture.GetMoleValue(MoleHelper.CondensationType(filterType)).Quantity / atmosphere.TotalMoles).ToDouble() < minRatio)
		{
			tooMix.Add(atmosphere.GasMixture.RemoveAll(MoleHelper.CondensationType(filterType)));
		}
		return result;
	}

	public static void MoveVolume(Atmosphere inputAtmos, Atmosphere outputAtmos, VolumeLitres volume, MatterState matterStateToMove, MoleQuantity minMolesToMove)
	{
		double num = RocketMath.Clamp(volume / inputAtmos.GetVolume(matterStateToMove), VolumeLitres.Zero, inputAtmos.GetVolume(matterStateToMove)).ToDouble();
		if (!(num <= 0.0))
		{
			MoleQuantity transferMoles = RocketMath.Max(minMolesToMove, inputAtmos.GasMixture.GetTotalMoles(matterStateToMove) * num);
			GasMixture gasMixture = inputAtmos.Remove(transferMoles, matterStateToMove);
			outputAtmos.Add(gasMixture);
		}
	}

	public static void MoveLiquidVolume(Atmosphere inputAtmos, Atmosphere outputAtmos, VolumeLitres volumeToMove)
	{
		outputAtmos.Add(RemoveLiquidVolume(inputAtmos, volumeToMove));
	}

	public static GasMixture RemoveLiquidVolume(Atmosphere inputAtmos, VolumeLitres volumeToRemove)
	{
		volumeToRemove /= inputAtmos.LiquidWorldVolumeScale;
		MoleQuantity transferMoles = ((inputAtmos.GasMixture.GetMolarVolumeLiquids() <= VolumeLitres.Zero) ? MoleQuantity.Zero : new MoleQuantity((volumeToRemove / inputAtmos.GasMixture.GetMolarVolumeLiquids()).ToDouble()));
		return inputAtmos.Remove(transferMoles, MatterState.Liquid);
	}

	public static void DrainLiquids(Atmosphere fromAtmos, Atmosphere toAtmos, VolumeLitres maxVolumeToMove, float safetyRatio = 0.99f)
	{
		if (fromAtmos != null && toAtmos != null)
		{
			double num = fromAtmos.LiquidWorldVolumeScale / toAtmos.LiquidWorldVolumeScale;
			VolumeLitres volumeLitres = RocketMath.Min(toAtmos.Volume * safetyRatio, toAtmos.Volume - Atmosphere.GetMinimumGasVolume(toAtmos.Mode) - toAtmos.TotalVolumeLiquids);
			VolumeLitres volumeToMove = RocketMath.Min(maxVolumeToMove, volumeLitres * num);
			MoveLiquidVolume(fromAtmos, toAtmos, volumeToMove);
		}
	}

	public static void EqualizeBothWays(Atmosphere inputAtmos, Atmosphere outputAtmos, MatterState matterState, PressurekPa pressurePerTick, MoleQuantity maxMoles, float scale = 1f)
	{
		if (inputAtmos == null || outputAtmos == null)
		{
			return;
		}
		Atmosphere atmosphere = outputAtmos;
		Atmosphere atmosphere2 = inputAtmos;
		if (inputAtmos.Volume < outputAtmos.Volume)
		{
			atmosphere = inputAtmos;
			atmosphere2 = outputAtmos;
		}
		Atmosphere atmosphere3 = inputAtmos;
		if (outputAtmos.PressureGassesAndLiquids > inputAtmos.PressureGassesAndLiquids)
		{
			inputAtmos = outputAtmos;
			outputAtmos = atmosphere3;
		}
		TemperatureKelvin temperature = ((atmosphere2.PressureGassesAndLiquids > PressurekPa.Zero) ? atmosphere2.Temperature : atmosphere.Temperature);
		PressurekPa pressurekPa = RocketMath.Min(pressurePerTick, inputAtmos.PressureGassesAndLiquids - outputAtmos.PressureGassesAndLiquids);
		if (pressurekPa > PressurekPa.Zero)
		{
			MoleQuantity moleQuantity = IdealGas.Quantity(pressurekPa, RocketMath.Min(inputAtmos.Volume, outputAtmos.Volume), temperature) * scale;
			if (moleQuantity <= MoleQuantity.Zero)
			{
				return;
			}
			moleQuantity = RocketMath.Min(maxMoles, moleQuantity);
			GasMixture gasMixture = inputAtmos.Remove(moleQuantity, matterState);
			if (!gasMixture.IsValid)
			{
				return;
			}
			outputAtmos.Add(gasMixture);
		}
		MoleEnergy moleEnergy = inputAtmos.GasMixture.TotalEnergy + outputAtmos.GasMixture.TotalEnergy;
		HeatCapacity heatCapacity = inputAtmos.GasMixture.HeatCapacity + outputAtmos.GasMixture.HeatCapacity;
		MoleEnergy moleEnergy2 = moleEnergy * (inputAtmos.GasMixture.HeatCapacity / heatCapacity).ToDouble();
		MoleEnergy totalEnergy = moleEnergy - moleEnergy2;
		inputAtmos.GasMixture.TotalEnergy = moleEnergy2;
		outputAtmos.GasMixture.TotalEnergy = totalEnergy;
	}

	public static VolumeLitres GetWorldVolume(Vector3 worldPosition)
	{
		return Chemistry.GridVolume * (1f - VoxelTerrain.GetDensityAtSize(worldPosition, 2));
	}

	public static bool CanContainAtmos(Grid3 grid)
	{
		Vector3 vector = grid.ToVector3() - Vector3.one;
		for (int i = 0; i < 8; i++)
		{
			float x = (float)MarchingConstants.VertexOffset[i, 0] + vector.x;
			float y = (float)MarchingConstants.VertexOffset[i, 1] + vector.y;
			float z = (float)MarchingConstants.VertexOffset[i, 2] + vector.z;
			if (VoxelTerrain.GetDensityAtSize(new Vector3(x, y, z).FloorToInt(), 1) < 0.49803922f)
			{
				return true;
			}
		}
		return false;
	}

	public static float LerpRate()
	{
		return Mathf.Lerp(0.2f, 1f, (float)AtmosphericsManager.WorldAtmospheresCount / 20000f);
	}

	public static float NewAtmosSupressionMultiplier()
	{
		return Mathf.Clamp((float)Mathf.Max(AtmosphericsManager.WorldAtmospheresCount, 2000) / 2000f, 1f, 10f);
	}

	public static MoleEnergy GetConvectionHeat(Atmosphere lhs, Atmosphere rhs, float area)
	{
		return new MoleEnergy(100.0 * (double)area * (lhs.Temperature - rhs.Temperature).ToDouble());
	}

	public static MoleEnergy GetRadiatedHeat(Atmosphere interior, Atmosphere exterior, float area, float emissivity = 1f)
	{
		double num = ((interior.Temperature > exterior.Temperature) ? 1 : (-1));
		double num2 = 0.0;
		lock (AtmosphericsManager.EntropyCurve)
		{
			num2 = AtmosphericsManager.EntropyCurve.Evaluate(Mathf.Abs((interior.Temperature - exterior.Temperature).ToFloat()));
		}
		return new MoleEnergy((double)(emissivity * area) * num2 * num);
	}

	public static MoleEnergy CalculateThingEntropy(Thing thing, Atmosphere worldAtmosphere, Atmosphere internalAtmosphere, double scale = 1.0)
	{
		return CalculateEntropy(worldAtmosphere, internalAtmosphere, thing.Position.ToGrid(), thing.RadiationFactor, thing.SurfaceArea, scale);
	}

	public static MoleEnergy CalculateEntropy(Atmosphere worldAtmosphere, Atmosphere internalAtmosphere, Grid3 gridPosition, float radiationFactor, float surfaceArea, double scale)
	{
		if (internalAtmosphere.Temperature <= AtmosphericsController.ReadonlyGlobalAtmosphere(gridPosition).Temperature)
		{
			return MoleEnergy.Zero;
		}
		TemperatureKelvin temperatureKelvin = ((AtmosphericsController.ReadonlyGlobalAtmosphere(gridPosition).Temperature < new TemperatureKelvin(1.0)) ? new TemperatureKelvin(50.0) : AtmosphericsController.ReadonlyGlobalAtmosphere(gridPosition).Temperature);
		TemperatureKelvin temperatureKelvin2 = temperatureKelvin;
		if (worldAtmosphere != null)
		{
			TemperatureKelvin temperature = worldAtmosphere.Temperature;
			if (temperatureKelvin < temperature)
			{
				MoleQuantity moleQuantity = IdealGas.Quantity(Chemistry.Limits.PressureMinimumSafe, worldAtmosphere.Volume, Chemistry.Temperature.ZeroDegrees);
				double t = Math.Clamp((worldAtmosphere.TotalMoles / moleQuantity).ToDouble(), 0.0, 1.0);
				temperatureKelvin2 = RocketMath.Lerp(temperatureKelvin, temperature, t);
			}
		}
		double num = 0.0;
		lock (AtmosphericsManager.EntropyCurve)
		{
			num = AtmosphericsManager.EntropyCurve.Evaluate((internalAtmosphere.Temperature - temperatureKelvin2).ToFloat());
		}
		return RocketMath.Clamp(new MoleEnergy((double)(radiationFactor * surfaceArea * internalAtmosphere.HeatExchangeRatio()) * scale * num), MoleEnergy.Zero, internalAtmosphere.GasMixture.TotalEnergy / 2.0);
	}

	public static MoleEnergy CalculateThingConvection(Thing thing, Atmosphere worldAtmosphere, Atmosphere internalAtmosphere, double scale = 1.0)
	{
		return CalculateConvection(worldAtmosphere, internalAtmosphere, thing.WorldGrid, thing.ConvectionFactor, thing.SurfaceArea, scale);
	}

	public static MoleEnergy CalculateConvection(Atmosphere worldAtmosphere, Atmosphere internalAtmosphere, WorldGrid wGrid, float convectionFactor, float surfaceArea, double scale)
	{
		if (!GridController.World.CanContainAtmos(wGrid))
		{
			return MoleEnergy.Zero;
		}
		MoleEnergy result = MoleEnergy.Zero;
		if (worldAtmosphere != null && (double)convectionFactor > 0.0)
		{
			result = GetConvectionHeat(internalAtmosphere, worldAtmosphere, surfaceArea * worldAtmosphere.HeatExchangeRatio() * internalAtmosphere.HeatExchangeRatio()) * AtmosphericsManager.Instance.TickSpeedSeconds * scale * convectionFactor;
		}
		return result;
	}

	public static int GetWorkerJobIndex(Grid3 grid)
	{
		double num = RocketMath.ModuloCorrect((double)grid.x / 10.0 / 2.0 + 0.5, 3.0);
		double num2 = RocketMath.ModuloCorrect((double)grid.y / 10.0 / 2.0 + 0.5, 3.0);
		double num3 = RocketMath.ModuloCorrect((double)grid.z / 10.0 / 2.0 + 0.5, 3.0);
		int num4 = (int)num;
		int num5 = (int)num2;
		int num6 = (int)num3;
		return num4 * 9 + num5 * 3 + num6;
	}

	public static void DoEntropy(Atmosphere internalAtmosphere, MoleEnergy energyToTransfer)
	{
		if (!(energyToTransfer <= MoleEnergy.Zero))
		{
			PlanetaryAtmosphereSimulation.AddEnergy(internalAtmosphere.GasMixture.RemoveEnergy(energyToTransfer));
		}
	}

	public static void DoConvection(Atmosphere internalAtmosphere, Atmosphere worldAtmosphere, MoleEnergy energyToTransfer, WorldGrid grid)
	{
		if (energyToTransfer.IsNaN())
		{
			return;
		}
		if (worldAtmosphere.Mode != AtmosphereMode.Global)
		{
			lock (worldAtmosphere)
			{
				internalAtmosphere.GasMixture.TransferEnergyTo(ref worldAtmosphere.GasMixture, energyToTransfer);
				worldAtmosphere.AtmosLifeState = AtmosLifeState.Active;
				return;
			}
		}
		Atmosphere atmosphere = AtmosphericsManager.CloneGlobalAtmosphereThreadSafe(grid);
		lock (atmosphere)
		{
			internalAtmosphere.GasMixture.TransferEnergyTo(ref atmosphere.GasMixture, energyToTransfer);
		}
	}

	public static void FreezeWorldAtmosphere(Atmosphere worldAtmosphere, GasMixture solidifiedGasses)
	{
		if (worldAtmosphere.Mode == AtmosphereMode.World)
		{
			worldAtmosphere.Remove(solidifiedGasses, MatterState.All);
			SpawnIces(worldAtmosphere, solidifiedGasses).Forget();
		}
	}

	private static async UniTaskVoid SpawnIces(Atmosphere atmosphere, GasMixture solidifiedGasses)
	{
		if (solidifiedGasses.GetTotalMolesGassesAndLiquids <= MoleQuantity.Zero)
		{
			return;
		}
		while (solidifiedGasses.GetTotalMolesGassesAndLiquids > MoleQuantity.Zero)
		{
			await UniTask.Delay(25, DelayType.DeltaTime, PlayerLoopTiming.FixedUpdate);
			if (GameManager.GameState != GameState.Running)
			{
				break;
			}
			GasMixture gasMixture = GasMixtureHelper.RemoveStackWorthOfFrozenGas(ref solidifiedGasses);
			if (gasMixture.GetTotalMolesGassesAndLiquids <= MoleQuantity.Zero)
			{
				solidifiedGasses.Reset();
			}
			else
			{
				SpawnIce(gasMixture, atmosphere.WorldPosition);
			}
		}
	}

	public static void SpawnIce(GasMixture gasMixture, Vector3 position)
	{
		CreateIcePrefab(gasMixture.Oxygen, position);
		CreateIcePrefab(gasMixture.Nitrogen, position);
		CreateIcePrefab(gasMixture.Pollutant, position);
		CreateIcePrefab(gasMixture.Steam, position);
		CreateIcePrefab(gasMixture.Methane, position);
		CreateIcePrefab(gasMixture.Water, position);
		CreateIcePrefab(gasMixture.PollutedWater, position);
		CreateIcePrefab(gasMixture.CarbonDioxide, position);
		CreateIcePrefab(gasMixture.LiquidNitrogen, position);
		CreateIcePrefab(gasMixture.LiquidOxygen, position);
		CreateIcePrefab(gasMixture.LiquidPollutant, position);
		CreateIcePrefab(gasMixture.LiquidMethane, position);
		CreateIcePrefab(gasMixture.NitrousOxide, position);
		CreateIcePrefab(gasMixture.LiquidCarbonDioxide, position);
		CreateIcePrefab(gasMixture.LiquidNitrousOxide, position);
		CreateIcePrefab(gasMixture.Hydrogen, position);
		CreateIcePrefab(gasMixture.LiquidHydrogen, position);
		CreateIcePrefab(gasMixture.Hydrazine, position);
		CreateIcePrefab(gasMixture.LiquidHydrazine, position);
		CreateIcePrefab(gasMixture.LiquidAlcohol, position);
		CreateIcePrefab(gasMixture.LiquidSodiumChloride, position);
		CreateIcePrefab(gasMixture.Silanol, position);
		CreateIcePrefab(gasMixture.LiquidSilanol, position);
		CreateIcePrefab(gasMixture.HydrochloricAcid, position);
		CreateIcePrefab(gasMixture.LiquidHydrochloricAcid, position);
		CreateIcePrefab(gasMixture.Ozone, position);
		CreateIcePrefab(gasMixture.LiquidOzone, position);
	}

	private static void CreateIcePrefab(Mole mole, Vector3 worldPosition)
	{
		if (!(mole.Quantity <= MoleQuantity.Zero) && mole.IsValid)
		{
			Vector3 vector = UnityEngine.Random.onUnitSphere * 0.5f;
			if (mole.PureIcePrefabHash == PrefabHashmap.Invalid || mole.PureIcePrefabHash == PrefabHashmap.Unassigned)
			{
				throw new ArgumentOutOfRangeException();
			}
			PureIce.AssignSpawnGasValues(OnServer.Create<PureIce>(mole.PureIcePrefabHash, worldPosition + vector, Quaternion.identity), mole.Type, mole.Quantity);
		}
	}

	public static bool IsNetworkUpdateRequired(byte toCheck, byte networkUpdateType)
	{
		return (networkUpdateType & toCheck) != 0;
	}

	public static void LinkThingAtmosphere(Thing thing, Atmosphere atmosphere)
	{
		if ((object)thing != null && atmosphere != null)
		{
			if (thing.InternalAtmosphere != null && thing.InternalAtmosphere != atmosphere)
			{
				thing.InternalAtmosphere.Thing = null;
			}
			atmosphere.Thing = thing;
			thing.InternalAtmosphere = atmosphere;
		}
	}

	public static bool TryRelinkThingAtmosphere(Atmosphere atmosphere)
	{
		if (atmosphere == null || atmosphere.Mode != AtmosphereMode.Thing)
		{
			return false;
		}
		if ((object)atmosphere.Thing != null || atmosphere.ParentThingReferenceId == 0L)
		{
			return false;
		}
		Thing thing = Referencable.Find<Thing>(atmosphere.ParentThingReferenceId);
		if ((object)thing == null)
		{
			return false;
		}
		if (thing.InternalAtmosphere != null && thing.InternalAtmosphere.ReferenceId != atmosphere.ReferenceId)
		{
			return false;
		}
		LinkThingAtmosphere(thing, atmosphere);
		return true;
	}

	public static void ReadStatic(RocketBinaryReader reader)
	{
		Network.ReadPackedId(reader, out var referenceId);
		byte b = reader.ReadByte();
		AtmosphereMode atmosphereMode = (AtmosphereMode)reader.ReadByte();
		Atmosphere atmosphere = null;
		if (referenceId > 0)
		{
			atmosphere = Referencable.Find<Atmosphere>(referenceId);
		}
		long referenceId2 = 0L;
		WorldGrid worldGrid = WorldGrid.INVALID;
		Vector3 direction = Vector3.zero;
		if (IsNetworkUpdateRequired(1, b))
		{
			Network.ReadPackedId(reader, out referenceId2);
		}
		if (atmosphereMode == AtmosphereMode.World || atmosphereMode == AtmosphereMode.Thing)
		{
			if (IsNetworkUpdateRequired(2, b))
			{
				worldGrid = reader.ReadWorldGrid();
			}
			if (IsNetworkUpdateRequired(8, b))
			{
				direction = reader.ReadVector3Half();
				if (atmosphere != null)
				{
					atmosphere.Direction = direction;
				}
			}
		}
		if (atmosphere == null && referenceId > 0)
		{
			switch (atmosphereMode)
			{
			case AtmosphereMode.World:
			{
				Atmosphere atmosphere2 = AtmosphericsManager.Find(worldGrid);
				if (atmosphere2 != null)
				{
					AtmosphericsManager.AllAtmospheres.Remove(atmosphere2);
				}
				atmosphere = new Atmosphere(worldGrid, referenceId)
				{
					Direction = direction
				};
				break;
			}
			case AtmosphereMode.Network:
			{
				AtmosphericsNetwork atmosphericsNetwork = Referencable.Find<AtmosphericsNetwork>(referenceId2);
				if (atmosphericsNetwork == null)
				{
					atmosphere = new Atmosphere();
					break;
				}
				atmosphere = new Atmosphere(atmosphericsNetwork, referenceId);
				atmosphericsNetwork.AssignAtmosphere(atmosphere);
				break;
			}
			case AtmosphereMode.Thing:
			{
				Thing thing = Referencable.Find<Thing>(referenceId2);
				if ((object)thing == null)
				{
					atmosphere = new Atmosphere(referenceId, new VolumeLitres(1.0))
					{
						ParentThingReferenceId = referenceId2
					};
				}
				else
				{
					atmosphere = new Atmosphere(thing, new VolumeLitres(1.0), referenceId)
					{
						ParentThingReferenceId = referenceId2
					};
					LinkThingAtmosphere(thing, atmosphere);
				}
				break;
			}
			default:
				atmosphere = new Atmosphere();
				break;
			}
		}
		if (atmosphere == null)
		{
			atmosphere = new Atmosphere();
		}
		atmosphere.ReferenceId = referenceId;
		atmosphere.Mode = atmosphereMode;
		atmosphere.Read(reader, b);
	}

	public static bool IsValidForNetworkSend(Atmosphere atmos)
	{
		if (atmos == null || atmos.ReferenceId <= 0)
		{
			return false;
		}
		if (atmos.BeingDestroyed)
		{
			return false;
		}
		if (atmos.IsNaN())
		{
			return false;
		}
		switch (atmos.Mode)
		{
		case AtmosphereMode.World:
			return atmos.AtmosphericsController.SampleGlobalAtmosphere(atmos.WorldGrid).ReferenceId == atmos.ReferenceId;
		case AtmosphereMode.Network:
			if (atmos.AtmosphericsNetwork != null && atmos.AtmosphericsNetwork.ReferenceId != 0L && atmos.AtmosphericsNetwork.Atmosphere != null)
			{
				return atmos.AtmosphericsNetwork.Atmosphere.ReferenceId == atmos.ReferenceId;
			}
			return false;
		case AtmosphereMode.Thing:
			if (atmos.Thing != null && atmos.Thing.ReferenceId != 0L && atmos.Thing.InternalAtmosphere != null)
			{
				return atmos.Thing.InternalAtmosphere.ReferenceId == atmos.ReferenceId;
			}
			return false;
		default:
			return false;
		}
	}

	public static float GetDensityMilliMolesPerLire(Atmosphere atmosphere)
	{
		return (float)IdealGas.GetMilliMolesPerLitre(atmosphere.Volume, atmosphere.TotalMolesGases);
	}

	public static bool IsSubmerged(Vector3 point)
	{
		if (GlobalAtmosphereLiquid.IsUnderGlobalLiquid(point))
		{
			return true;
		}
		WorldGrid worldGrid = new WorldGrid(point);
		Atmosphere atmosphere = AtmosphericsManager.Find(worldGrid);
		if (atmosphere == null || atmosphere.TotalVolumeLiquids <= LiquidSolver.RenderThreshold(atmosphere))
		{
			return false;
		}
		float num = (float)((worldGrid.Value.y - 10) / 10) + atmosphere.LiquidVolumeRatio * 2f;
		return point.y < num;
	}

	public static bool IsSubmerged(Vector3 point, Atmosphere atmosphere, bool onlyWhenLiquidRendered = true)
	{
		if (GlobalAtmosphereLiquid.IsUnderGlobalLiquid(point))
		{
			return true;
		}
		VolumeLitres volumeLitres = (onlyWhenLiquidRendered ? LiquidSolver.RenderThreshold(atmosphere) : VolumeLitres.Zero);
		if (atmosphere == null || atmosphere.TotalVolumeLiquids <= volumeLitres)
		{
			return false;
		}
		float num = (float)((atmosphere.WorldGrid.Value.y - 10) / 10) + atmosphere.LiquidVolumeRatio * 2f;
		if (!(point.y <= num))
		{
			return GlobalAtmosphereLiquid.IsUnderGlobalLiquid(point);
		}
		return true;
	}
}
