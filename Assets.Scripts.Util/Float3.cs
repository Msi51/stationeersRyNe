using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts.Util;

public struct Float3
{
	[XmlAttribute]
	public float x;

	[XmlAttribute]
	public float y;

	[XmlAttribute]
	public float z;

	public Float3(float vx, float vy, float vz)
	{
		x = vx;
		y = vy;
		z = vz;
	}

	public Float3(Vector3 v)
	{
		x = v.x;
		y = v.y;
		z = v.z;
	}

	public Vector3 ToVector3()
	{
		return new Vector3(x, y, z);
	}
}
