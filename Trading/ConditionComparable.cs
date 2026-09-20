using System;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Util;

namespace Trading;

public abstract class ConditionComparable : ConditionData
{
	[XmlAttribute("Compare")]
	public CompareOperator CompareOperator = CompareOperator.EqualOrGreater;

	public override int GetChecksum()
	{
		return (int)(((uint)base.GetChecksum() ^ (uint)CompareOperator) * 41);
	}

	protected bool Compare(int a, int b)
	{
		switch (CompareOperator)
		{
		case CompareOperator.Less:
			return a < b;
		case CompareOperator.EqualOrLess:
			return a <= b;
		case CompareOperator.Equal:
			return a == b;
		case CompareOperator.Unassigned:
		case CompareOperator.EqualOrGreater:
			return a >= b;
		case CompareOperator.Greater:
			return a > b;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	protected bool Compare(float a, float b)
	{
		switch (CompareOperator)
		{
		case CompareOperator.Less:
			return a < b;
		case CompareOperator.EqualOrLess:
			return a <= b;
		case CompareOperator.Equal:
			return RocketMath.Approximately(a, b);
		case CompareOperator.Unassigned:
		case CompareOperator.EqualOrGreater:
			return a >= b;
		case CompareOperator.Greater:
			return a > b;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
