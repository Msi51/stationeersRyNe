using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Pipes;

public class LiquidRocketEngine : RocketEngineBase
{
	public float minFuelFlow = 0.25f;

	public float maxFuelFlow = 1.25f;

	public VolumeLitres MinFuelFlow => new VolumeLitres(minFuelFlow);

	public VolumeLitres MaxFuelFlow => new VolumeLitres(maxFuelFlow);

	protected override void PrepareThrustSimulation(Atmosphere input1, Atmosphere input2)
	{
		TemperatureKelvin temperatureKelvin = new TemperatureKelvin(125.0);
		MoleQuantity moleQuantity = RocketMath.NumberOfMolesLiquid(input1.Volume * Thing.StressedRatio, Chemistry.GasType.LiquidMethane);
		GasMixture gasMixture = GasMixtureHelper.Create();
		gasMixture.Add(new Mole(Chemistry.GasType.LiquidMethane, moleQuantity * 0.6660000085830688, MoleEnergy.Zero));
		gasMixture.Add(new Mole(Chemistry.GasType.LiquidOxygen, moleQuantity * 0.33399999141693115, MoleEnergy.Zero));
		gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, temperatureKelvin);
		input1.Add(gasMixture);
	}

	protected override void MovePropellant(Atmosphere internalAtmosphere, Atmosphere input1, Atmosphere input2)
	{
		AtmosphereHelper.MoveLiquidVolume(input1, internalAtmosphere, RocketMath.Lerp(MinFuelFlow, MaxFuelFlow, base.OutputSetting / MaxSetting));
		AtmosphereHelper.MoveVolume(input1, internalAtmosphere, RocketMath.Lerp(MinFuelFlow, MaxFuelFlow, base.OutputSetting / MaxSetting), AtmosphereHelper.MatterState.Gas, MoleQuantity.Zero);
		base.PassedMoles = internalAtmosphere.TotalMoles;
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new LiquidRocketEngineSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is LiquidRocketEngineSaveData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		_ = savedData is LiquidRocketEngineSaveData;
	}
}
