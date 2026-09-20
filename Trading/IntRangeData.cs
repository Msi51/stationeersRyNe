using System;
using System.Xml.Serialization;

namespace Trading;

public class IntRangeData : IChecksum
{
	[XmlAttribute("Value")]
	public int Value = 1;

	[XmlAttribute("Min")]
	public int Min = 1;

	[XmlAttribute("Max")]
	public int Max = -1;

	public int GenerateValue(Random random)
	{
		if (Max < 1 || Min < 1 || Min > Max)
		{
			if (Value <= 0)
			{
				return 1;
			}
			return Value;
		}
		return random.Next(Min, Max);
	}

	public virtual int GetChecksum()
	{
		return Value ^ Min ^ Max;
	}
}
