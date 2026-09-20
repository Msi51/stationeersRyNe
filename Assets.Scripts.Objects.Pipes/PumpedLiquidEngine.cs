using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;

namespace Assets.Scripts.Objects.Pipes;

public class PumpedLiquidEngine : RocketEngineBase
{
	private static readonly VolumeLitres MAXFuelFlow = new VolumeLitres(0.550000011920929);

	public override float EngineEfficiency => 35f;

	public float Ratio1 => base.OutputSetting;

	public float Ratio2 => 100f - base.OutputSetting;

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
		TemperatureKelvin t = new TemperatureKelvin(125.0);
		base.OutputSetting = 75f;
		MoleQuantity quantity = RocketMath.NumberOfMolesLiquid(input1.Volume * Thing.StressedRatio, Chemistry.GasType.LiquidAlcohol);
		MoleQuantity quantity2 = RocketMath.NumberOfMolesLiquid(input2.Volume * Thing.StressedRatio, Chemistry.GasType.LiquidOxygen);
		GasMixture gasMixture = GasMixtureHelper.Create();
		gasMixture.Add(new Mole(Chemistry.GasType.LiquidAlcohol, quantity, MoleEnergy.Zero));
		gasMixture.TotalEnergy = new MoleEnergy(gasMixture.HeatCapacity, Chemistry.Temperature.ZeroDegrees);
		GasMixture gasMixture2 = GasMixtureHelper.Create();
		gasMixture2.Add(new Mole(Chemistry.GasType.LiquidOxygen, quantity2, MoleEnergy.Zero));
		gasMixture2.TotalEnergy = new MoleEnergy(gasMixture2.HeatCapacity, t);
		input1.Add(gasMixture);
		input2.Add(gasMixture2);
	}

	protected override void MovePropellant(Atmosphere internalAtmosphere, Atmosphere input1, Atmosphere input2)
	{
		float num = base.Throttle / _maxThrottle * (Ratio1 / 100f);
		float num2 = base.Throttle / _maxThrottle * (Ratio2 / 100f);
		VolumeLitres volumeToMove = MAXFuelFlow * num;
		VolumeLitres volumeToMove2 = MAXFuelFlow * num2;
		AtmosphereHelper.MoveLiquidVolume(input1, internalAtmosphere, volumeToMove);
		AtmosphereHelper.MoveLiquidVolume(input2, internalAtmosphere, volumeToMove2);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new PumpedLiquidEngineSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		_ = savedData is PumpedLiquidEngineSaveData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		_ = savedData is PumpedLiquidEngineSaveData;
	}
}
