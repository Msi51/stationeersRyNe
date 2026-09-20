using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceInputOutputImportExportCircuitSaveData))]
public class CombustionCentrifugeSaveData : DeviceInputOutputImportExportCircuitSaveData
{
	[XmlElement]
	public float Throttle;

	[XmlElement]
	public float CombustionLimiter;

	[XmlElement]
	public float Rpm;

	[XmlElement]
	public float Stress;
}
