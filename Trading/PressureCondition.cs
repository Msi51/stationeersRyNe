using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using Networks;

namespace Trading;

public class PressureCondition : ConditionComparable
{
	[XmlAttribute("kPa")]
	public float PressurekPa = float.NaN;

	public override string DebugName => $"Pressure {CompareOperator} {PressurekPa}kPa";

	public override int GetChecksum()
	{
		return (base.GetChecksum() ^ (int)(PressurekPa * 1000f)) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		bool flag = false;
		float num = float.NaN;
		if (t is Thing { InternalAtmosphere: not null } thing)
		{
			num = thing.InternalAtmosphere.PressureGasses.ToFloat();
		}
		else if (t is Atmosphere { PressureGasses: var pressureGasses })
		{
			num = pressureGasses.ToFloat();
		}
		else if (t is Thing thing2 && thing2 is INetworkedAtmospherics networkedAtmospherics)
		{
			num = ((AtmosphericsNetwork)networkedAtmospherics.StructureNetwork).Atmosphere.PressureGasses.ToFloat();
		}
		else if (t is Room { AverageGasMixture: { IsValid: not false } } room)
		{
			num = room.Pressure.ToFloat();
		}
		if (!float.IsNaN(PressurekPa) && !float.IsNaN(num) && Compare(num, PressurekPa))
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
		if (!(PressurekPa < 0f))
		{
			for (int i = 0; i < generations; i++)
			{
				stringBuilder.Append("    ");
			}
			stringBuilder.AppendLine(GameStrings.PressureCondition.AsString(value.AsColor("white"), PressurekPa.ToStringPrefix("Pa", "yellow")));
		}
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
