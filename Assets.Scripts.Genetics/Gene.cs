using System.Xml.Serialization;

namespace Assets.Scripts.Genetics;

public enum Gene
{
	[XmlEnum("None")]
	None,
	[XmlEnum("GrowthTimeMultiplier")]
	GrowthSpeedMultiplier,
	[XmlEnum("DarkPerDay")]
	DarkPerDay,
	[XmlEnum("LightPerDay")]
	LightPerDay,
	[XmlEnum("DroughtTolerance")]
	DroughtTolerance,
	[XmlEnum("WaterUsage")]
	WaterUsage,
	[XmlEnum("LowPressureResistance")]
	LowPressureResistance,
	[XmlEnum("LowTemperatureResistance")]
	LowTemperatureResistance,
	[XmlEnum("UndesiredGasTolerance")]
	UndesiredGasTolerance,
	[XmlEnum("GasProduction")]
	GasProduction,
	[XmlEnum("HighPressureResistance")]
	HighPressureResistance,
	[XmlEnum("HighTemperatureResistance")]
	HighTemperatureResistance,
	[XmlEnum("SuffocationTolerance")]
	SuffocationTolerance,
	[XmlEnum("LowPressureTolerance")]
	LowPressureTolerance,
	[XmlEnum("LowTemperatureTolerance")]
	LowTemperatureTolerance,
	[XmlEnum("HighPressureTolerance")]
	HighPressureTolerance,
	[XmlEnum("HighTemperatureTolerance")]
	HighTemperatureTolerance,
	[XmlEnum("UndesiredGasResistance")]
	UndesiredGasResistance,
	[XmlEnum("LightTolerance")]
	LightTolerance,
	[XmlEnum("DarknessTolerance")]
	DarknessTolerance
}
