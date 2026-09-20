using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;

namespace Assets.Scripts.Atmospherics;

public class InternalAtmosphereConditioner : GameBase, IInternalConditionerHandler
{
	[ReadOnly]
	public Thing Thing;

	private static float EnergyCoolPowerCostPercent = 0.01f;

	private static float EnergyHeatPowerCostPercent = 0.5f;

	private MoleEnergy _desiredEnergyDelta;

	private MoleEnergy _desiredEnergy;

	private MoleEnergy _usedEnergy;

	private const float cooledJoulesPerMole = 2000f;

	private IInternalConditioner _parent;

	public double Efficiency => _parent.Efficiency;

	public MoleEnergy MaxEnergy => _parent.GetMaxEnergy();

	public PressurekPa WasteMaxPressure => _parent.GetWasteMaxPressure();

	public float OutputSetting => _parent.GetOutputSetting();

	public TemperatureKelvin OutputTemperature => _parent.GetOutputTemperature();

	public PressurekPa PressurePerTick => _parent.GetPressurePerTick();

	public bool OnOff => _parent.OnOff;

	public int Exporting => _parent.Exporting;

	public int Importing => _parent.Importing;

	public BatteryCell Battery => _parent.Battery;

	public GasCanister WasteTank => _parent.WasteTank;

	public GasCanister AirTank => _parent.AirTank;

	public Atmosphere InternalAtmosphere
	{
		get
		{
			return _parent.GetInternalAtmosphere();
		}
		set
		{
			_parent.SetInternalAtmosphere(value);
		}
	}

	private void Awake()
	{
		_parent = Thing as IInternalConditioner;
		_parent.SetConditioningHandler(this);
	}

	public MoleQuantity SetGasToTank(MoleQuantity minimumMolesToMove)
	{
		if (Exporting == 0)
		{
			return MoleQuantity.Zero;
		}
		if (Battery == null || Battery.IsEmpty || WasteTank == null)
		{
			return MoleQuantity.Zero;
		}
		if (WasteTank.IsOpen)
		{
			OnServer.Interact(WasteTank.InteractOpen, 0);
		}
		PressurekPa pressureGassesAndLiquids = WasteTank.InternalAtmosphere.PressureGassesAndLiquids;
		if (pressureGassesAndLiquids >= WasteMaxPressure)
		{
			return MoleQuantity.Zero;
		}
		if (pressureGassesAndLiquids < PressurekPa.One)
		{
			FillWaste();
		}
		HandleFilters();
		MoleQuantity val = IdealGas.Quantity(RocketMath.Min(PressurePerTick * Efficiency, InternalAtmosphere.PressureGassesAndLiquids - new PressurekPa(OutputSetting)), WasteTank.InternalAtmosphere.Volume, InternalAtmosphere.Temperature);
		if (InternalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids < Chemistry.MINIMUM_VALID_TOTAL_MOLES)
		{
			val = InternalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids;
		}
		Battery.PowerStored -= 10f;
		GasMixture gasMixture = InternalAtmosphere.Remove(RocketMath.Min(RocketMath.Max(minimumMolesToMove, val), InternalAtmosphere.TotalMoles), AtmosphereHelper.MatterState.All);
		WasteTank.InternalAtmosphere.Add(gasMixture);
		return gasMixture.GetTotalMolesGasses;
	}

	public void FillWaste()
	{
		if ((bool)AirTank && (bool)WasteTank)
		{
			if (AirTank.IsOpen)
			{
				OnServer.Interact(AirTank.InteractOpen, 0);
			}
			if (WasteTank.IsOpen)
			{
				OnServer.Interact(WasteTank.InteractOpen, 0);
			}
			PressurekPa val = PressurePerTick / (AirTank.InternalAtmosphere.Volume / Chemistry.PipeVolume).ToDouble();
			MoleQuantity transferMoles = IdealGas.Quantity(RocketMath.Min(PressurekPa.One - WasteTank.InternalAtmosphere.PressureGassesAndLiquids, val), WasteTank.InternalAtmosphere.Volume, AirTank.InternalAtmosphere.Temperature);
			GasMixture gasMixture = AirTank.InternalAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.Gas);
			WasteTank.InternalAtmosphere.Add(gasMixture);
		}
	}

	public virtual void HandleFilters()
	{
		List<Slot> filterSlots = _parent.GetFilterSlots();
		for (int i = 0; i < filterSlots.Count; i++)
		{
			GasFilter gasFilter = filterSlots[i].Occupant as GasFilter;
			if ((bool)gasFilter)
			{
				gasFilter.FilterGas(ref InternalAtmosphere.GasMixture, ref WasteTank.InternalAtmosphere.GasMixture);
			}
		}
	}

	public MoleQuantity AirConditioning(Atmosphere selectedAtmosphere)
	{
		if (!OnOff)
		{
			return MoleQuantity.Zero;
		}
		if (Battery == null || WasteTank == null || Battery.IsEmpty)
		{
			return MoleQuantity.Zero;
		}
		if (WasteTank.IsOpen)
		{
			OnServer.Interact(WasteTank.InteractOpen, 0);
		}
		if (WasteTank.InternalAtmosphere.PressureGassesAndLiquids >= WasteMaxPressure)
		{
			return MoleQuantity.Zero;
		}
		_desiredEnergy = IdealGas.Energy(selectedAtmosphere.GasMixture.HeatCapacity, OutputTemperature);
		_desiredEnergyDelta = _desiredEnergy - selectedAtmosphere.GasMixture.TotalEnergy;
		_usedEnergy = RocketMath.Abs(RocketMath.Clamp(_desiredEnergyDelta, -MaxEnergy * Efficiency, MaxEnergy * Efficiency));
		if (_desiredEnergyDelta < MoleEnergy.Zero)
		{
			Battery.PowerStored -= (_usedEnergy * EnergyCoolPowerCostPercent).ToFloat();
			_usedEnergy = selectedAtmosphere.GasMixture.RemoveEnergy(_usedEnergy);
			WasteTank.InternalAtmosphere.GasMixture.AddEnergy(_usedEnergy);
			return new MoleQuantity(_usedEnergy.ToDouble() / 2000.0);
		}
		Battery.PowerStored -= (_usedEnergy * EnergyHeatPowerCostPercent).ToFloat();
		selectedAtmosphere.GasMixture.AddEnergy(_usedEnergy);
		return MoleQuantity.Zero;
	}

	public MoleQuantity GetGasFromTank()
	{
		if (Importing == 0 || AirTank == null)
		{
			return MoleQuantity.Zero;
		}
		if (AirTank.InternalAtmosphere.PressureGassesAndLiquids < new PressurekPa(0.001))
		{
			return MoleQuantity.Zero;
		}
		if (InternalAtmosphere == null)
		{
			return MoleQuantity.Zero;
		}
		MoleQuantity transferMoles = IdealGas.Quantity(RocketMath.Min(PressurePerTick * Efficiency, new PressurekPa(OutputSetting) - InternalAtmosphere.PressureGassesAndLiquids), InternalAtmosphere.Volume, AirTank.InternalAtmosphere.Temperature);
		if (AirTank.InternalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids < Chemistry.MINIMUM_VALID_TOTAL_MOLES)
		{
			transferMoles = AirTank.InternalAtmosphere.GasMixture.GetTotalMolesGassesAndLiquids;
		}
		GasMixture gasMixture = AirTank.InternalAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.Gas);
		InternalAtmosphere.Add(gasMixture);
		return gasMixture.GetTotalMolesGasses;
	}
}
