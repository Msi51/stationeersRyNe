using System.Xml.Serialization;
using UnityEngine;

[XmlRoot]
public class GradientColorKeyData
{
	public Color Color;

	public float Time;

	public GradientColorKeyData()
	{
	}

	public GradientColorKeyData(Color color, float time)
	{
		Color = color;
		Time = time;
	}
}
