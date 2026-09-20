using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Trading.Waypoints;

[XmlInclude(typeof(WaypointSaveData))]
public class WaypointSaveData : StructureSaveData
{
	[XmlElement]
	public long TargetWaypointReferenceId;

	[XmlElement]
	public int WaypointHeight;

	[XmlElement]
	public bool VisualiserOn;
}
