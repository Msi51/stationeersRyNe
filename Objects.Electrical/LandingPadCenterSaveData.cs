using System.Xml.Serialization;

namespace Objects.Electrical;

[XmlInclude(typeof(LandingPadModularSaveData))]
public class LandingPadCenterSaveData : LandingPadModularSaveData
{
	[XmlElement]
	public long TraderReferenceID;

	[XmlElement]
	public long ParentMotherboardID;

	[XmlElement]
	public long NextWaypointReferenceId;

	[XmlElement]
	public float VirtualWaypointHeight;
}
