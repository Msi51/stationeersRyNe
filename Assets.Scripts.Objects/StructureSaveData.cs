using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects;

[XmlInclude(typeof(ThingSaveData))]
public class StructureSaveData : ThingSaveData
{
	[XmlElement]
	public int CurrentBuildState;

	[XmlElement]
	public long MothershipReferenceId;

	[XmlElement]
	public bool HasSpawnedWreckage;

	[XmlElement]
	public Vector3 RegisteredWorldPosition;

	[XmlElement]
	public Quaternion RegisteredWorldRotation;

	[XmlElement]
	public RocketRecordData RocketRecord;
}
