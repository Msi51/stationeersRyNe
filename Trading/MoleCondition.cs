using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

[XmlType("Moles")]
public class MoleCondition : ConditionComparable
{
	[XmlAttribute("Value")]
	public float Value = float.NaN;

	public override string DebugName => $"Moles {Value} mol";

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ (int)Value) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		float num = float.NaN;
		bool flag = false;
		if (t is Thing thing)
		{
			num = thing.InternalAtmosphere.TotalMoles.ToFloat();
		}
		else if (t is Atmosphere { TotalMoles: var totalMoles })
		{
			num = totalMoles.ToFloat();
		}
		else if (typeof(T) == typeof(GasMixture))
		{
			GasMixture gasMixture = __refvalue(__makeref(t), GasMixture);
			num = gasMixture.GetTotalMoles(AtmosphereHelper.MatterState.All).ToFloat();
		}
		if (!double.IsNaN(num) && !float.IsNaN(Value) && Compare(num, Value))
		{
			flag = true;
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
		stringBuilder.AppendLine(GameStrings.TradeGasQuantityComparison.AsString(value.AsColor("white"), Value.ToStringPrefix("mol", "yellow")));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
