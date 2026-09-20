using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

[XmlRoot]
public class ThingModData
{
	public string PrefabName;

	public float SurfaceAreaScale = float.NaN;

	public float ThermodynamicsScale = float.NaN;

	public float SolarHeatingScale = float.NaN;

	public float ShatterTemperature = float.NaN;

	public float FlashpointTemperature = float.NaN;

	public float AutoignitionTemperature = float.NaN;

	public float BurnTime = float.NaN;

	public float EnergyReleasedWhenBurning = float.NaN;
}
