using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class SurvivalPropertyCondition : ConditionComparable
{
	[XmlAttribute("Type")]
	public EntitySurvivalProperty SurvivalProperty;

	[XmlAttribute("Percent")]
	public float PercentValue = float.NaN;

	[XmlAttribute("Ratio")]
	public float RatioValue = float.NaN;

	public override string DebugName
	{
		get
		{
			string obj = ((SurvivalProperty != EntitySurvivalProperty.None) ? ("Property " + EnumCollections.EntitySurvivalProperty.GetName(SurvivalProperty)) : "");
			string text = ((PercentValue != 0f) ? ("Value " + StringManager.Get(PercentValue) + "%") : "");
			return obj + " " + text;
		}
	}

	public override int GetChecksum()
	{
		return (int)(((((uint)base.GetChecksum() ^ (uint)SurvivalProperty) * 41) ^ (uint)PercentValue.GetHashCode()) * 41);
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Entity entity && SurvivalProperty != EntitySurvivalProperty.None)
		{
			float survivalPropertyRatio = entity.GetSurvivalPropertyRatio(SurvivalProperty);
			if (!float.IsNaN(PercentValue))
			{
				flag = Compare(survivalPropertyRatio, PercentValue / 100f);
			}
			else if (!float.IsNaN(RatioValue))
			{
				flag = Compare(survivalPropertyRatio, RatioValue);
			}
		}
		if (flag)
		{
			return base.Evaluate(t);
		}
		return false;
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
