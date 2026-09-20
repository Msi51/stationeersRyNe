using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Pipes;

public class ElectrolysisMachine : DeviceInputOutputCircuit, IThermal
{
	[Header("Electrolyser")]
	[SerializeField]
	[FormerlySerializedAs("Volume")]
	private float volume = 1000f;

	[Tooltip("Temperature of the output gas")]
	public static TemperatureKelvin OutputTemperature = Chemistry.Temperature.TwentyDegrees;

	[Tooltip("How much of the electrical energy consumed is used to electrolyze. > 1 adds more energy than electrical draw. Doing so might cause perpetual motion machines.")]
	public static float ElectricEfficiency = 40f;

	public static float ActiveUsedPower = 3600f;

	public static float IdleUsedPower = 50f;

	public VolumeLitres Volume => new VolumeLitres(volume);

	protected override bool IsOperable
	{
		get
		{
			if (!base.IsStructureCompleted)
			{
				return false;
			}
			bool flag = (object)base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = (base.IsInputValid || base.IsInput2Valid) && base.IsOutputValid && !flag;
			if (Error == 1)
			{
				if (!flag2)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (flag2)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public override bool HasReadableAtmosphere => false;

	public override CanConstructInfo CanConstruct()
	{
		if (HasFrameBelow())
		{
			return base.CanConstruct();
		}
		return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
	}

	public override void AssessError()
	{
		bool flag = !base.IsInputValid && !base.IsInput2Valid;
		bool flag2 = !base.IsOutputValid;
		bool flag3 = (object)base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
		if (GameManager.RunSimulation && HasErrorState && Error == 0 && (flag || flag2 || flag3))
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if ((GameManager.RunSimulation && HasErrorState && Error == 1 && !flag && !flag2 && !flag3) || !OnOff || !Powered)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere != null)
		{
			AtmosphericsManager.Deregister(base.InternalAtmosphere);
			base.InternalAtmosphere = null;
		}
	}

	public override void OnAtmosphericTick()
	{
		UsedPower = ActiveUsedPower;
		if (!OnOff || !Powered)
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			UsedPower = IdleUsedPower;
			base.ProcessedMoles = MoleQuantity.Zero;
			return;
		}
		if (!IsOperable || Mode == 0 || (InputNetwork?.Atmosphere != null && InputNetwork.Atmosphere.GasMixture.Water.Quantity <= MoleQuantity.Zero))
		{
			if (Activate == 1)
			{
				OnServer.Interact(base.InteractActivate, 0);
			}
			UsedPower = IdleUsedPower;
			base.ProcessedMoles = MoleQuantity.Zero;
			return;
		}
		GasMixture gasMixture = GasMixtureHelper.Create();
		CombustionResult result = Combustion.GetResult(Chemistry.GasType.Hydrogen, Chemistry.GasType.Oxygen);
		double num = result.FuelMoleCount.ToDouble();
		double num2 = result.OxidiserMoleCount.ToDouble();
		double num3 = result.Outputs[0].Quantity.ToDouble();
		float num4 = ElectricEfficiency * UsedPower;
		MoleEnergy moleEnergy = new MoleEnergy((Mole.SpecificHeat(Chemistry.GasType.Hydrogen).ToDouble() * num + Mole.SpecificHeat(Chemistry.GasType.Oxygen).ToDouble() * num2) * OutputTemperature.ToDouble() + Mole.Enthalpy(Chemistry.GasType.Hydrogen) * num + Mole.LatentHeatOfVaporization(Chemistry.GasType.Steam) * num3) / num3;
		MoleEnergy moleEnergy2 = IdealGas.EnergyPerMole(InputNetwork.Atmosphere.GasMixture.Temperature, Mole.SpecificHeat(Chemistry.GasType.Water));
		MoleEnergy moleEnergy3 = moleEnergy - moleEnergy2;
		MoleQuantity quantity = new MoleQuantity((moleEnergy3 > MoleEnergy.Zero) ? (num4 / moleEnergy3.ToFloat()) : float.MaxValue);
		base.ProcessedMoles = InputNetwork.Atmosphere.GasMixture.Remove(Chemistry.GasType.Water, quantity).Quantity;
		MoleQuantity moleQuantity = base.ProcessedMoles / num3;
		gasMixture.Oxygen.Add(moleQuantity * num2, MoleEnergy.Zero);
		gasMixture.Hydrogen.Add(moleQuantity * num, MoleEnergy.Zero);
		gasMixture.TotalEnergy = IdealGas.Energy(gasMixture.HeatCapacity, OutputTemperature);
		OutputNetwork.Atmosphere.Add(gasMixture);
		if (Activate == 0)
		{
			OnServer.Interact(base.InteractActivate, 1);
		}
	}
}
