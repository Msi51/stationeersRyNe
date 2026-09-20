using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceImportExportSaveData))]
public class RocketMinerSaveData : DeviceImportExportSaveData
{
	[XmlElement("MiningProgress")]
	public float MiningProgress;

	[XmlElement("LastMinedPrefab")]
	public string LastMinedPrefab;

	[XmlElement("QuantityMined")]
	public int QuantityMined;
}
