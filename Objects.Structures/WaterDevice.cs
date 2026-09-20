using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Pipes;

namespace Objects.Structures;

public abstract class WaterDevice : DeviceInputOutput
{
	protected static PressurekPa MinimumWorldPressure = new PressurekPa(30.0);

	protected bool WaterTooHot
	{
		get
		{
			if (InputNetwork?.Atmosphere != null)
			{
				return InputNetwork?.Atmosphere.Temperature > Chemistry.Temperature.ZeroDegrees + new TemperatureKelvin(100.0);
			}
			return false;
		}
	}

	protected bool WaterTooCold
	{
		get
		{
			if (InputNetwork?.Atmosphere != null)
			{
				return InputNetwork?.Atmosphere.Temperature < Chemistry.Temperature.ZeroDegrees;
			}
			return false;
		}
	}

	protected bool WaterPolluted
	{
		get
		{
			if (InputNetwork?.Atmosphere != null)
			{
				if (!(InputNetwork.Atmosphere.GasMixture.TotalToxins > MoleQuantity.Zero))
				{
					return InputNetwork.Atmosphere.GasMixture.GetTotalMolesLiquids > InputNetwork.Atmosphere.GasMixture.Water.Quantity;
				}
				return true;
			}
			return false;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (base.IsStructureCompleted && base.IsInputValid && base.IsOutputValid && !WaterTooCold && !WaterTooHot && !WaterPolluted && !OutputFull)
			{
				return IsMinimumWorldPressure;
			}
			return false;
		}
	}

	protected bool OutputFull => OutputNetwork.Atmosphere.TotalVolumeLiquids > OutputNetwork.Atmosphere.Volume - VolumeLitres.One;

	protected bool IsMinimumWorldPressure
	{
		get
		{
			Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
			if (atmosphere == null)
			{
				return false;
			}
			return atmosphere.PressureGassesAndLiquids >= MinimumWorldPressure;
		}
	}

	protected bool HasEnoughWater(MoleQuantity requiredMoles)
	{
		if (InputNetwork?.Atmosphere != null)
		{
			return InputNetwork.Atmosphere.TotalMolesLiquids >= requiredMoles;
		}
		return false;
	}
}
