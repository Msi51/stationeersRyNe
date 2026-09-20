using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts.Objects;

[XmlInclude(typeof(ThingSaveData))]
public class DynamicThingSaveData : ThingSaveData
{
	[XmlElement]
	public long ParentReferenceId;

	[XmlElement]
	public int ParentSlotId;

	[XmlElement]
	public bool Dragged;

	[XmlElement]
	public Vector3 DragOffset;

	[XmlElement]
	public Vector3 Velocity;

	[XmlElement]
	public Vector3 AngularVelocity;

	[XmlElement]
	public float HealthCurrent;
}
