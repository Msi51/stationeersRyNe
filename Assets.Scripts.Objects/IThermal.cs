using Assets.Scripts.Atmospherics;

namespace Assets.Scripts.Objects;

public interface IThermal
{
	float ConvectionFactor { get; }

	float RadiationFactor { get; }

	float SolarHeatingFactor { get; }

	float EnergyRadiated { get; set; }

	float EnergyConvected { get; set; }

	Atmosphere ThermalAtmosphere { get; }

	string DisplayName { get; }
}
