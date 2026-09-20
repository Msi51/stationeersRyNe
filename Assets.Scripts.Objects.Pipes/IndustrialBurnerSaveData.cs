using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(FurnaceSaveData))]
public class IndustrialBurnerSaveData : FurnaceSaveData
{
	public float Stress;

	public bool StressedToFailure;

	public double MachineTemperature;
}
