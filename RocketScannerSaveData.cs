using System.Xml.Serialization;
using Assets.Scripts.Objects;

[XmlInclude(typeof(StructureSaveData))]
public class RocketScannerSaveData : StructureSaveData
{
	[XmlElement("CurrentCycleTime")]
	public float CurrentCycleTime;
}
