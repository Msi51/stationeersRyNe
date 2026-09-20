using System.Xml.Serialization;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Objects.Electrical;

public class HorizontalQuarrySaveData : DeviceImportExportSaveData
{
	[XmlElement]
	public Vector3 DrillCarPosition;

	[XmlElement]
	public QuarryCollectedOre[] CollectedOre;
}
