using System.Xml.Serialization;
using UnityEngine;

[XmlRoot("CelestialSprite")]
public class CelestialSpriteReference : CelestialBodyReference
{
	[XmlElement("Material")]
	public StarData StarData;

	[XmlAttribute]
	public float Magnitude = 1f;

	[XmlElement("Distance")]
	public ValueRange Distance;

	public float GetBrightness()
	{
		if (StarData == null || float.IsNaN(StarData.Brightness))
		{
			return 1f;
		}
		return StarData.Brightness;
	}

	public float GetMinimum()
	{
		if (StarData == null || float.IsNaN(StarData.Minimum))
		{
			return 0f;
		}
		return StarData.Minimum;
	}

	public float GetMagnitude()
	{
		return Mathf.Clamp(Magnitude, 0.02f, 5f);
	}

	public override void Create(CelestialBody body, CelestialBodyTemplate template)
	{
		CelestialSprite.AssignFromPool(body, template, this);
	}
}
