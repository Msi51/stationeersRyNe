using System.Xml.Serialization;
using UnityEngine;

namespace ThingImport;

public class EdgeData
{
	[XmlAttribute("x1")]
	public float X1;

	[XmlAttribute("y1")]
	public float Y1;

	[XmlAttribute("z1")]
	public float Z1;

	[XmlAttribute("x2")]
	public float X2;

	[XmlAttribute("y2")]
	public float Y2;

	[XmlAttribute("z2")]
	public float Z2;

	[XmlIgnore]
	public Vector3 Point1;

	[XmlIgnore]
	public Vector3 Point2;

	public void Initialize()
	{
		Point1 = new Vector3(X1, Y1, Z1);
		Point2 = new Vector3(X2, Y2, Z2);
	}
}
