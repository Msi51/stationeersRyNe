using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using UnityEngine;

public class Vector2Reference
{
	protected const string X_ATTRIBUTE = "x";

	[XmlAttribute("x")]
	public float x;

	protected const string Y_ATTRIBUTE = "y";

	[XmlAttribute("y")]
	public float y = 1f;

	public Vector3 ToVector2()
	{
		return new Vector3(x, y);
	}

	public virtual void Add(ref XElement parentElement, string spawnPositionElement)
	{
		XElement element = XDocumentHelper.MakeElement(spawnPositionElement, ref parentElement);
		XDocumentHelper.SetAttribute(element, "x", x.ToString("F4"));
		XDocumentHelper.SetAttribute(element, "y", y.ToString("F4"));
	}
}
