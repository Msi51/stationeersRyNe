using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class GrowthStateCondition : ConditionComparable
{
	[XmlAttribute("Value")]
	public int Value;

	[XmlAttribute("IsPlanted")]
	public bool IsPlanted = true;

	public override string DebugName => $"Growth State {Value}";

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ Value) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		if (t is Plant plant)
		{
			if (Compare(plant.Stage, Value))
			{
				flag = true;
			}
			flag = flag && plant.IsPlanted == IsPlanted;
		}
		if (flag)
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
		stringBuilder.AppendLine(GameStrings.GrowthStateCondition.AsString(value.AsColor("white"), StringManager.Get(Value)));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
