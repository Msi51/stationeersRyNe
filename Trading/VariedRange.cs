using System.Xml.Serialization;
using UnityEngine;

namespace Trading;

public class VariedRange : VarianceData
{
	[XmlAttribute("Value")]
	public float Value;

	[XmlAttribute("Step")]
	public float Step = float.NaN;

	public override float Apply(float value)
	{
		if (Mathf.Abs(Value) <= 0.01f)
		{
			return value;
		}
		value += Random.Range(0f - Value, Value);
		if (!float.IsNaN(Step))
		{
			value = Mathf.Round(value / Step) * Step;
		}
		return value;
	}

	public override int GetChecksum()
	{
		return (int)(Value * 1000f) ^ (int)(Step * 1000f);
	}
}
