using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

[XmlType("Decay")]
public class DecayCondition : ConditionComparable
{
	[XmlAttribute("Value")]
	public float Value;

	public override string DebugName => $"Decay {Value}";

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ (int)(Value * 1000f)) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		if (!(t is IPerishable perishable))
		{
			return base.Evaluate(t);
		}
		float decay = perishable.GetDecay();
		if (!Compare(decay, Value))
		{
			return false;
		}
		return base.Evaluate(t);
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		string value = CompareOperator.DisplayString();
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.AppendLine(GameStrings.TradeDecay.AsString(value.AsColor("white"), Value.ToStringPercent("yellow")));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
