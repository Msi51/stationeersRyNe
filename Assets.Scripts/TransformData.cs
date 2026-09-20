using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts;

public class TransformData
{
	[XmlAttribute("x")]
	public float PosX;

	[XmlAttribute("y")]
	public float PosY;

	[XmlAttribute("z")]
	public float PosZ;

	[XmlAttribute("rx")]
	public float RotX;

	[XmlAttribute("ry")]
	public float RotY;

	[XmlAttribute("rz")]
	public float RotZ;

	public Vector3 Position()
	{
		return new Vector3(PosX, PosY, PosZ);
	}

	public Quaternion Rotation()
	{
		return Quaternion.Euler(RotX, RotY, RotZ);
	}
}
