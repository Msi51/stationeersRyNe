using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using Networks;

namespace Trading;

[XmlType("Quantity")]
public class QuantityCondition : ConditionComparable
{
	[XmlAttribute("Value")]
	public float Quantity = 1f;

	public override string DebugName => $"Quantity {Quantity}";

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ (int)(Quantity * 1000f)) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		float a = 0f;
		if (t is Thing thing && thing is IQuantity quantity)
		{
			a = quantity.GetQuantity;
		}
		else if (t is Atmosphere { TotalMoles: var totalMoles })
		{
			a = totalMoles.ToFloat();
		}
		else if (typeof(T) == typeof(GasMixture))
		{
			GasMixture gasMixture = __refvalue(__makeref(t), GasMixture);
			a = gasMixture.GetTotalMoles().ToFloat();
		}
		else if (t is Room room)
		{
			a = room.Grids.Count;
		}
		else if (t is StructureNetwork structureNetwork)
		{
			a = structureNetwork.StructureList.Count;
		}
		if (Compare(a, Quantity))
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
		GameStrings.TradeQuantity.AppendFormat(stringBuilder, value.AsColor("white"), StringManager.Get(Quantity).AsColor("yellow"));
		stringBuilder.AppendLine();
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
