using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts;

public class ColorFloat4Reference : ColorReference
{
	[XmlAttribute("r")]
	public float Red;

	[XmlAttribute("g")]
	public float Green;

	[XmlAttribute("b")]
	public float Blue;

	[XmlAttribute("a")]
	public float Alpha;

	public ColorFloat4Reference()
	{
	}

	public ColorFloat4Reference(Color color)
		: base(color)
	{
		Red = color.r;
		Green = color.g;
		Blue = color.b;
		Alpha = color.a;
	}

	public override Color ToColor()
	{
		return new Color(Red, Green, Blue, Alpha);
	}

	public override int GetChecksum()
	{
		return (((((((int)(Red * 1000f) * 41) ^ (int)Green) * 41) ^ (int)Blue) * 41) ^ (int)Alpha) * 41;
	}
}
