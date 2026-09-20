using Assets.Scripts.Atmospherics;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class LungsZrilian : Lungs
{
	protected override TemperatureKelvin TemperatureMin => Chemistry.Temperature.ZeroDegrees - new TemperatureKelvin(20.0);

	protected override TemperatureKelvin TemperatureMax => Chemistry.Temperature.ZeroDegrees + new TemperatureKelvin(80.0);

	protected override PressurekPa ToxinLevel => base.InternalAtmosphere.PartialPressureZrillianToxins;

	public override float AtmosphericEfficiency => Mathf.Clamp((base.InternalAtmosphere.PartialPressureMethane / Chemistry.MinimumOxygenPartialPressure).ToFloat(), 0f, 1.5f);
}
