using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Pipes;

public class GovernedGasEngine : RocketEngineBase
{
	public const float MAX_MOLAR_INPUT = 18f;

	public const float EFFICIENCY_GOVERNED_GAS = 25f;

	public override float EngineEfficiency => 25f;

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted)
			{
				return false;
			}
			bool flag = base.IsInput1Valid && base.GridController.CanContainAtmos(new WorldGrid(_flamePosition));
			if (GameManager.RunSimulation && HasErrorState && Error == 0 && !flag)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (GameManager.RunSimulation && HasErrorState && Error == 1 && flag)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag;
		}
	}

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
		MoleQuantity transferMoles = new MoleQuantity(RocketMath.Lerp(0.0, 18.0, (double)base.Throttle / 100.0));
		GasMixture gasMixture = input1.Remove(transferMoles, AtmosphereHelper.MatterState.All);
		base.PassedMoles = gasMixture.GetTotalMolesGassesAndLiquids;
		internalAtmosphere.Add(gasMixture);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => false, 
			LogicType.Maximum => false, 
			LogicType.Ratio => false, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new GovernedGasEngineSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is GovernedGasEngineSaveData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		_ = savedData is GovernedGasEngineSaveData;
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		_ = interactable.Action;
		_ = 36;
	}
}
