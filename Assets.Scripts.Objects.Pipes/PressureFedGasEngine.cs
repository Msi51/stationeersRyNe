using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Pipes;

public class PressureFedGasEngine : PressureFedEngine
{
	public override float EngineEfficiency => 24f;

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted)
			{
				return false;
			}
			bool flag = base.IsInput1Valid && base.IsInput2Valid && base.GridController.CanContainAtmos(new WorldGrid(_flamePosition));
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
		input2.Add(gasMixture);
	}

	protected override void MovePropellant(Atmosphere internalAtmosphere, Atmosphere input1, Atmosphere input2)
	{
		MoveGas(input1, internalAtmosphere, base.Throttle, base.PressurePerTick, Pipe.ContentType.Gas);
		MoveGas(input2, internalAtmosphere, base.Throttle, base.PressurePerTick, Pipe.ContentType.Gas);
		base.PassedMoles = internalAtmosphere.TotalMoles;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting || logicType - 23 <= LogicType.Power)
		{
			return false;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}
}
