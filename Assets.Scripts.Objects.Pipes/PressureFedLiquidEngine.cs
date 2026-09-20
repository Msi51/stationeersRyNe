using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class PressureFedLiquidEngine : PressureFedEngine
{
	protected virtual float HeatExchangeAreaMax => 3f;

	protected virtual float FlowRateMin => 0.04f;

	protected virtual float FlowRateMax => 0.8f;

	public override float EngineEfficiency => 40f;

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
		PressurekPa pressure = Chemistry.Limits.MAXPressureLiquidPipe * Thing.StressedRatio;
		TemperatureKelvin temperatureKelvin = new TemperatureKelvin(125.0);
		MoleQuantity moleQuantity = RocketMath.NumberOfMolesLiquid(input1.Volume * Thing.StressedRatio, Chemistry.GasType.LiquidMethane);
		GasMixture gasMixture = GasMixtureHelper.Create();
		gasMixture.Add(new Mole(Chemistry.GasType.LiquidMethane, moleQuantity * 0.699999988079071, MoleEnergy.Zero));
		gasMixture.Add(new Mole(Chemistry.GasType.LiquidOxygen, moleQuantity * 0.30000001192092896, MoleEnergy.Zero));
		gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, temperatureKelvin);
		input1.Add(gasMixture);
		VolumeLitres gasVolume = input1.GetGasVolume();
		MoleQuantity moleQuantity2 = RocketMath.NumberOfMolesGas(pressure, gasVolume, temperatureKelvin);
		GasMixture gasMixture2 = GasMixtureHelper.Create();
		gasMixture2.Add(new Mole(Chemistry.GasType.Methane, moleQuantity2 * 0.6660000085830688, MoleEnergy.Zero));
		gasMixture2.Add(new Mole(Chemistry.GasType.Oxygen, moleQuantity2 * 0.33399999141693115, MoleEnergy.Zero));
		gasMixture2.TotalEnergy = IdealGas.Energy(gasMixture2.HeatCapacity, temperatureKelvin);
		input1.Add(gasMixture2);
	}

	protected override void MovePropellant(Atmosphere internalAtmosphere, Atmosphere input1, Atmosphere input2)
	{
		float value = RocketMath.MapToScale(0f, Chemistry.Limits.MAXPressureLiquidPipe.ToFloat(), FlowRateMin, FlowRateMax, input1.PressureGasses.ToFloat());
		value = Mathf.Clamp(value, FlowRateMin, FlowRateMax);
		value *= base.Throttle / 100f;
		if (value > 0f)
		{
			AtmosphereHelper.MoveLiquidVolume(input1, internalAtmosphere, new VolumeLitres(value));
			MoveGas(input1, internalAtmosphere, base.Throttle, base.PressurePerTick, Pipe.ContentType.Liquid);
		}
		base.PassedMoles = internalAtmosphere.TotalMoles;
	}

	public override void OnPreAtmosphere()
	{
		base.OnPreAtmosphere();
		_inputNetwork2?.Atmosphere?.StateChange();
		HandleHeatExchange();
	}

	private void HandleHeatExchange()
	{
		Atmosphere atmosphere = _inputNetwork2?.Atmosphere;
		if (atmosphere != null)
		{
			float num = base.OutputSetting / 100f * HeatExchangeAreaMax;
			MoleEnergy convectionHeat = AtmosphereHelper.GetConvectionHeat(atmosphere, base.InternalAtmosphere, num * HeatExchangeRatio(atmosphere, base.InternalAtmosphere));
			atmosphere.GasMixture.TransferEnergyTo(ref base.InternalAtmosphere.GasMixture, convectionHeat * AtmosphericsManager.Instance.TickSpeedSeconds);
		}
	}

	private static float HeatExchangeRatio(Atmosphere input, Atmosphere output)
	{
		float num = input.HeatExchangeRatio();
		float num2 = output.HeatExchangeRatio();
		return num * num2;
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (_inputConnection1 != null && _inputConnection1.Collider != null && hitCollider == _inputConnection1.Collider)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = GameStrings.LiquidFuelInput.DisplayString;
			return result;
		}
		if (_inputConnection2 != null && _inputConnection2.Collider != null && hitCollider == _inputConnection2.Collider)
		{
			PassiveTooltip result = new PassiveTooltip(true);
			result.Title = GameStrings.HeatExchangerLiquidInput.DisplayString;
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}
}
