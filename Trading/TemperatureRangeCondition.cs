using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class TemperatureRangeCondition : ConditionData
{
	[XmlAttribute("Unit")]
	public TemperatureType TemperatureType = TemperatureType.Celsius;

	[XmlAttribute("Min")]
	public float Min;

	[XmlAttribute("Max")]
	public float Max = 20f;

	public override string DebugName => TemperatureType switch
	{
		TemperatureType.Kelvin => $"Temperature {Min} to {Max} K", 
		TemperatureType.Celsius => $"Temperature {Min} to {Max} °C", 
		_ => "Temperature ???", 
	};

	public override int GetChecksum()
	{
		return (((base.GetChecksum() ^ (int)Min) * 41) ^ (int)Max) * 41;
	}

	public override bool Evaluate<T>(T t)
	{
		TemperatureKelvin temperatureKelvin = TemperatureKelvin.Zero;
		if (t is Thing thing)
		{
			temperatureKelvin = thing.InternalAtmosphere?.Temperature ?? TemperatureKelvin.Zero;
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
		TemperatureKelvin temperatureKelvin2 = ((TemperatureType == TemperatureType.Kelvin) ? new TemperatureKelvin(Min) : RocketMath.CelsiusToKelvin(Min));
		TemperatureKelvin temperatureKelvin3 = ((TemperatureType == TemperatureType.Kelvin) ? new TemperatureKelvin(Max) : RocketMath.CelsiusToKelvin(Max));
		if (temperatureKelvin >= temperatureKelvin2 && temperatureKelvin <= temperatureKelvin3)
		{
			return base.Evaluate(t);
		}
		return false;
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		stringBuilder.AppendLine(GameStrings.TradeTemperatureBetween.AsString(Min.ToStringPrefix("°C", "yellow"), Max.ToStringPrefix("°C", "yellow")));
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
