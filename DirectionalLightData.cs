using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

public class DirectionalLightData
{
	[XmlElement("Color", typeof(ColorFloat4Reference))]
	[XmlElement("Color32", typeof(Color32Reference))]
	public ColorReference Color;

	[XmlElement("Intensity")]
	public FloatRangeData Intensity;

	[XmlElement("Frequency")]
	public FloatReference Frequency = new FloatReference(1f);

	private OpenSimplexNoise _noise = new OpenSimplexNoise();

	public float GetIntensity()
	{
		return RocketMath.MapToScale(-1f, 1f, Intensity.Min, Intensity.Max, _noise.Evaluate(0f, Time.time * (float)Frequency));
	}
}
