using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using UnityEngine;

public class Vector3Reference : Vector2Reference
{
	private const string Z_ATTRIBUTE = "z";

	[XmlAttribute("z")]
	public float z;

	public static implicit operator Vector3(Vector3Reference vector3Reference)
	{
		return vector3Reference.ToVector3();
	}

	public Vector3 ToVector3()
	{
		return new Vector3(x, y, z);
	}

	public Quaternion ToQuaternion()
	{
		return Quaternion.Euler(ToVector3());
	}

	public Vector3Reference()
	{
	}

	public Vector3Reference(Vector3 value)
	{
		x = value.x;
		y = value.y;
		z = value.z;
	}

	public override void Add(ref XElement parentElement, string spawnPositionElement)
	{
		XElement element = XDocumentHelper.MakeElement(spawnPositionElement, ref parentElement);
		XDocumentHelper.SetAttribute(element, "x", x.ToString("F4"));
		XDocumentHelper.SetAttribute(element, "y", y.ToString("F4"));
		XDocumentHelper.SetAttribute(element, "z", z.ToString("F4"));
	}
}
