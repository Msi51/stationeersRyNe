using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceInputOutputCircuitSaveData))]
public class IndustrialCombustorSaveData : DeviceInputOutputCircuitSaveData
{
	public bool StressedToFailure;

	public double MachineTemperature;
}
