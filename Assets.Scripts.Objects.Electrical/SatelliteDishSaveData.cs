using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(RotatableSaveData))]
public class SatelliteDishSaveData : RotatableSaveData
{
	[XmlElement]
	public int Setting;

	[XmlElement]
	public int TargetPadIndex;

	[XmlElement]
	public long BestSignalIDFilter = -1L;
}
