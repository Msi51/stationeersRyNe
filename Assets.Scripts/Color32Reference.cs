using System.Xml.Serialization;
using UnityEngine;

namespace Assets.Scripts;

public class Color32Reference : ColorReference
{
	[XmlAttribute("r")]
	public byte Red;

	[XmlAttribute("g")]
	public byte Green;

	[XmlAttribute("b")]
	public byte Blue;

	[XmlAttribute("a")]
	public byte Alpha;

	public override Color ToColor()
	{
		return new Color((float)(int)Red / 255f, (float)(int)Green / 255f, (float)(int)Blue / 255f, (float)(int)Alpha / 255f);
	}

	public override int GetChecksum()
	{
		return ((((((Red * 1000 * 41) ^ Green) * 41) ^ Blue) * 41) ^ Alpha) * 41;
	}
}
