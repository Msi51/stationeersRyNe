using System.Xml.Serialization;
using UnityEngine;

[XmlRoot("Color")]
public class ColorRGB
{
	[XmlAttribute]
	public float r = 1f;

	[XmlAttribute]
	public float g = 1f;

	[XmlAttribute]
	public float b = 1f;

	public Color ToColor()
	{
		return new Color(r, g, b, 1f);
	}

	public static implicit operator Color(ColorRGB color)
	{
		return color.ToColor();
	}

	public ColorRGB()
	{
	}

	public ColorRGB(Color color)
	{
		r = color.r;
		g = color.g;
		b = color.b;
	}

	public ColorRGB(float r, float g, float b)
	{
		this.r = r;
		this.g = g;
		this.b = b;
	}

	public ColorRGB(byte r, byte g, byte b)
	{
		this.r = (float)(int)r / 255f;
		this.g = (float)(int)g / 255f;
		this.b = (float)(int)b / 255f;
	}

	public ColorRGB(byte l)
	{
		r = (float)(int)l / 255f;
		g = (float)(int)l / 255f;
		b = (float)(int)l / 255f;
	}
}
