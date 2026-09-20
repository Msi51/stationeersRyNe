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

public class TemperatureComparableCondition : ConditionComparable
{
	[XmlAttribute("Celsius")]
	public float Celsius = float.NaN;

	[XmlAttribute("Kelvin")]
	public float Kelvin = float.NaN;

	public override string DebugName
	{
		get
		{
			if (!float.IsNaN(Celsius))
			{
				return $"Temperature {Celsius} °C";
			}
			return $"Temperature {Kelvin} K";
		}
	}

	public override int GetChecksum()
	{
		int num = base.GetChecksum();
		if (!float.IsNaN(Celsius))
		{
			num = (num ^ (int)(Celsius * 1000f)) * 41;
		}
		if (!float.IsNaN(Kelvin))
		{
			num = (num ^ (int)(Kelvin * 1000f)) * 41;
		}
		return num;
	}

	public override bool Evaluate<T>(T t)
	{
		TemperatureKelvin temperatureKelvin = TemperatureKelvin.Zero;
		if (t is Thing { InternalAtmosphere: not null } thing)
		{
			temperatureKelvin = thing.InternalAtmosphere.Temperature;
		}
		else if (t is Thing thing2 && thing2 is INetworkedAtmospherics networkedAtmospherics)
		{
			temperatureKelvin = ((AtmosphericsNetwork)networkedAtmospherics.StructureNetwork).Atmosphere.Temperature;
		}
		else if (t is Atmosphere atmosphere)
		{
			temperatureKelvin = atmosphere.Temperature;
		}
		else if (typeof(T) == typeof(GasMixture))
		{
			GasMixture gasMixture = __refvalue(__makeref(t), GasMixture);
			temperatureKelvin = gasMixture.Temperature;
		}
		else if (t is Room room)
		{
			temperatureKelvin = room.Temperature;
		}
		if (!float.IsNaN(Celsius))
		{
			if (Compare(temperatureKelvin.ToFloat(), RocketMath.CelsiusToKelvin(Celsius).ToFloat()))
			{
				return base.Evaluate(t);
			}
			return false;
		}
		if (!float.IsNaN(Kelvin))
		{
			if (Compare(temperatureKelvin.ToFloat(), Kelvin))
			{
				return base.Evaluate(t);
			}
			return false;
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
		if (!float.IsNaN(Celsius))
		{
			stringBuilder.AppendLine(GameStrings.TemperatureComparableCondition.AsString(value.AsColor("white"), Celsius.ToStringPrefix("°C", "yellow")));
		}
		if (!float.IsNaN(Kelvin))
		{
			stringBuilder.AppendLine(GameStrings.TemperatureComparableCondition.AsString(value.AsColor("white"), Kelvin.ToStringPrefix("K", "yellow")));
		}
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
