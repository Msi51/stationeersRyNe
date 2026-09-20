using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class SolarPanelSaveData : StructureSaveData
{
	[XmlElement]
	public double Horizontal;

	[XmlElement]
	public double Vertical;

	[XmlElement]
	public double TargetHorizontal;

	[XmlElement]
	public double TargetVertical;
}
