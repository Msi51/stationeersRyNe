using Assets.Scripts.Atmospherics;

namespace Assets.Scripts.Objects.Clothing.Suits;

public class AdvancedACSuit : SuitBase
{
	public override TemperatureKelvin MaxCoolantTemperatureK => new TemperatureKelvin(323.15);

	public override TemperatureKelvin MinCoolantTemperatureK => Chemistry.FREEZING_TEMPERATURE_NITROGEN_K;

	public override float MaxACEnergy => 1000f;

	public override float EnergyCoolingPowerCostPercent => 0.2f;

	public override float EnergyHeatingPowerCostPercent => 0.7f;
}
