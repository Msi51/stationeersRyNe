using System.Xml.Serialization;
using UnityEngine;

namespace ThingImport.Thumbnails;

public class ThumbnailRotationPreset
{
	[XmlAttribute]
	public string Name;

	[XmlAttribute]
	public float X;

	[XmlAttribute]
	public float Y;

	[XmlAttribute]
	public float Z;

	[XmlAttribute]
	public float Zoom = 1f;

	[XmlIgnore]
	public Vector3 Euler => new Vector3(X, Y, Z);
}
