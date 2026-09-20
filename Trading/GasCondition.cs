using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;
using Networks;

namespace Trading;

public class GasCondition : ConditionComparable, IGasTrade
{
	[XmlAttribute("Type")]
	public Chemistry.GasType GasType;

	[XmlAttribute("Percent")]
	public int Percent = -1;

	[XmlAttribute("Moles")]
	public int Moles = -1;

	[XmlAttribute("PartialPressure")]
	public float PartialPressure = float.NaN;

	public const int UNDEFINED = -1;

	public override string DebugName
	{
		get
		{
			if (Moles >= 0)
			{
				return $"Gas {GasType} {CompareOperator} {Moles} mol";
			}
			if (Percent >= 0)
			{
				return $"Gas {GasType} {CompareOperator} {Percent}%%";
			}
			if (PartialPressure >= 0f)
			{
				return $"Gas {GasType} {CompareOperator} {PartialPressure}kPa";
			}
			return $"Gas {GasType} {CompareOperator} {Percent}%% {Moles}mol";
		}
	}

	public Chemistry.GasType GetGasType()
	{
		return GasType;
	}

	public override int GetChecksum()
	{
		return (int)(((((((((uint)base.GetChecksum() ^ (uint)GasType) * 41) ^ (uint)(Percent * 1000)) * 41) ^ (uint)(Moles * 1000)) * 41) ^ (uint)(int)(PartialPressure * 1000f)) * 41);
	}

	private void GetGasValues(GasMixture gasMixture, out float percent, out float moles)
	{
		Chemistry.GasType gasType = MoleHelper.EvaporationType(GasType);
		Chemistry.GasType gasType2 = MoleHelper.CondensationType(GasType);
		percent = gasMixture.GetGasTypeRatio(GasType) * 100f;
		moles = gasMixture.GetGasMoles(GasType).ToFloat();
		if (gasType != Chemistry.GasType.Undefined)
		{
			percent += gasMixture.GetGasTypeRatio(gasType) * 100f;
			moles += gasMixture.GetGasMoles(gasType).ToFloat();
		}
		if (gasType2 != Chemistry.GasType.Undefined)
		{
			percent += gasMixture.GetGasTypeRatio(gasType2) * 100f;
			moles += gasMixture.GetGasMoles(gasType2).ToFloat();
		}
	}

	public override bool Evaluate<T>(T t)
	{
		float percent = float.NaN;
		float moles = float.NaN;
		float num = float.NaN;
		bool flag = false;
		if (t is Thing { InternalAtmosphere: not null } thing)
		{
			GetGasValues(thing.InternalAtmosphere.GasMixture, out percent, out moles);
			num = thing.InternalAtmosphere.PartialPressure(GasType).ToFloat();
		}
		else if (t is Atmosphere atmosphere)
		{
			GetGasValues(atmosphere.GasMixture, out percent, out moles);
			num = atmosphere.PartialPressure(GasType).ToFloat();
		}
		else if (t is Thing thing2 && thing2 is INetworkedAtmospherics networkedAtmospherics)
		{
			Atmosphere atmosphere2 = ((AtmosphericsNetwork)networkedAtmospherics.StructureNetwork).Atmosphere;
			GetGasValues(atmosphere2.GasMixture, out percent, out moles);
			num = atmosphere2.PartialPressure(GasType).ToFloat();
		}
		else if (t is PipeNetwork { Atmosphere: var atmosphere3 })
		{
			GetGasValues(atmosphere3.GasMixture, out percent, out moles);
			num = atmosphere3.PartialPressure(GasType).ToFloat();
		}
		else if (typeof(T) == typeof(GasMixture))
		{
			GasMixture gasMixture = __refvalue(__makeref(t), GasMixture);
			GetGasValues(gasMixture, out percent, out moles);
		}
		else if (t is Room { AverageGasMixture: { IsValid: not false } } room)
		{
			GetGasValues(room.GasMixture, out percent, out moles);
			num = room.PartialPressure(GasType).ToFloat();
		}
		if (Percent != -1 && !float.IsNaN(percent))
		{
			if (Compare(percent, Percent))
			{
				flag = true;
			}
		}
		else if (Moles != -1 && !float.IsNaN(moles))
		{
			if (Compare(moles, Moles))
			{
				flag = true;
			}
		}
		else if (!float.IsNaN(PartialPressure) && !float.IsNaN(num) && Compare(num, PartialPressure))
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
		if (Percent != -1 || Moles != -1 || !(PartialPressure < 0f))
		{
			for (int i = 0; i < generations; i++)
			{
				stringBuilder.Append("    ");
			}
			string arg = Localization.GetName(GasType).AsColor("#44AD83");
			if (Percent != -1)
			{
				stringBuilder.AppendLine(GameStrings.TradeRatio.AsString(arg, value.AsColor("white"), Percent.ToStringPercent("yellow")));
			}
			if (Moles != -1)
			{
				stringBuilder.AppendLine(GameStrings.TradeRatio.AsString(arg, value.AsColor("white"), Moles.ToStringPrefix("mol", "yellow")));
			}
			if (PartialPressure > 0f)
			{
				stringBuilder.AppendLine(GameStrings.PartialPressureCondition.AsString(arg, value.AsColor("white"), PartialPressure.ToStringPrefix("kPa", "yellow")));
			}
		}
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
