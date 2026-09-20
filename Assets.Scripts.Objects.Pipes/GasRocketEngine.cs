using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class GasRocketEngine : RocketEngineBase
{
	[SerializeField]
	private float maxMolarInput = 25f;

	[SerializeField]
	private float minMolarInput = 5f;

	protected override void PrepareThrustSimulation(Atmosphere input1, Atmosphere input2)
	{
		PressurekPa pressure = Chemistry.Limits.MAXPressureGasPipe * Thing.StressedRatio;
		TemperatureKelvin temperatureKelvin = new TemperatureKelvin(215.0);
		MoleQuantity moleQuantity = RocketMath.NumberOfMolesGas(pressure, input1.Volume, temperatureKelvin);
		GasMixture gasMixture = GasMixtureHelper.Create();
		gasMixture.Add(new Mole(Chemistry.GasType.Methane, moleQuantity * 0.6660000085830688, MoleEnergy.Zero));
		gasMixture.Add(new Mole(Chemistry.GasType.Oxygen, moleQuantity * 0.33399999141693115, MoleEnergy.Zero));
		gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, temperatureKelvin);
		input1.Add(gasMixture);
	}

	protected override void MovePropellant(Atmosphere internalAtmosphere, Atmosphere input1, Atmosphere input2)
	{
		MoleQuantity transferMoles = new MoleQuantity(RocketMath.MapToScale(MinSetting, MaxSetting, minMolarInput, maxMolarInput, base.OutputSetting));
		GasMixture gasMixture = input1.Remove(transferMoles, AtmosphereHelper.MatterState.All);
		base.PassedMoles = gasMixture.GetTotalMolesGassesAndLiquids;
		internalAtmosphere.Add(gasMixture);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new GasRocketEngineSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is GasRocketEngineSaveData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		_ = savedData is GasRocketEngineSaveData;
	}
}
