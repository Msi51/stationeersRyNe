using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceImportExportSaveData))]
public class RocketGasCollectorSaveData : DeviceImportExportSaveData
{
	[XmlElement("MiningProgress")]
	public float MiningProgress;

	[XmlElement("QuantityMined")]
	public int QuantityMined;
}
