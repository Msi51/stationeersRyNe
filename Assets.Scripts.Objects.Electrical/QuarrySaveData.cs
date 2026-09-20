using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class QuarrySaveData : DeviceImportExportSaveData
{
	[XmlElement]
	public Vector3 DrillEndSgementX;

	[XmlElement]
	public Vector3 DrillEndSgementY;

	[XmlElement]
	public Vector3 DrillEndSgementZ;

	[XmlElement]
	public Vector3 SavedDrillEndSegmentX;

	[XmlElement]
	public Vector3 SavedDrillEndSegmentY;

	[XmlElement]
	public Vector3 SavedDrillEndSegmentZ;

	[XmlElement]
	public Vector3 HeadPosition;

	[XmlElement]
	public Vector3 CarriagePosition;

	[XmlElement]
	public Vector3 RailingPosition;

	[XmlElement]
	public Vector3 MovementAmount;

	[XmlElement]
	public bool ForwardDirection;

	[XmlElement]
	public bool DrillFinished;

	[XmlElement]
	public bool IsFinishing;

	[XmlElement]
	public bool TransportingOre;

	[XmlElement]
	public QuarryCollectedOre[] CollectedOre;
}
