using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using Objects.Items;
using UnityEngine;

namespace Trading;

[XmlType("Percent")]
public class PercentCondition : ConditionComparable
{
	[XmlAttribute("Value")]
	public float Percent = 100f;

	public override string DebugName => $"Percent {Percent}";

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ (int)(Percent * 1000f)) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		float a = 0f;
		if (t is Consumable consumable)
		{
			a = Mathf.Round(consumable.GetRatioQuantity * 100f);
		}
		if (Compare(a, Percent))
		{
			return base.Evaluate(t);
		}
		return false;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		string value = CompareOperator.DisplayString();
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.AppendLine(GameStrings.TradeQuantity.AsString(value.AsColor("white"), Percent.ToStringPercent("yellow")));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
