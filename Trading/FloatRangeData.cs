using System;
using System.Xml.Serialization;

namespace Trading;

public class FloatRangeData
{
	[XmlAttribute("Value")]
	public float Value = float.NaN;

	[XmlAttribute("Min")]
	public float Min;

	[XmlAttribute("Max")]
	public float Max = 1f;

	public float GenerateValue(Random random)
	{
		if (!float.IsNaN(Value))
		{
			return Value;
		}
		if (Min > Max)
		{
			return 1f;
		}
		return (float)((double)Min + (double)(Max - Min) * random.NextDouble());
	}
}
