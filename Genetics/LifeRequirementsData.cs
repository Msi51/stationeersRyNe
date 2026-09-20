using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;

namespace Genetics;

public class LifeRequirementsData : DataCollection
{
	[XmlElement("LiquidPerTick")]
	public GasQuantityData LiquidPerTick;

	[XmlElement("Inhaled")]
	public List<GasQuantityRatioData> InhaledGases;

	[XmlElement("Exhaled")]
	public List<GasQuantityData> ExhaledGases;

	[XmlElement("HarmfulGas")]
	public List<GasPressureData> HarmfulGases;

	[XmlElement("WaterUsage")]
	public PlantStatData WaterUsage;

	[XmlElement("GasProduction")]
	public PlantStatData GasProduction;

	[XmlElement("GrowthSpeedMultiplier")]
	public PlantStatData GrowthSpeedMultiplier;

	[XmlElement("TimeUntilDehydrationDamage")]
	public PlantStatData TimeUntilDehydrationDamage;

	[XmlElement("TimeUntilUndesiredGasDamage")]
	public PlantStatData TimeUntilUndesiredGasDamage;

	[XmlElement("UndesiredGasResistance")]
	public PlantStatData UndesiredGasResistance;

	[XmlElement("TimeUntilFrozenDamage")]
	public PlantStatData TimeUntilFrozenDamage;

	[XmlElement("TimeUntilOverHeatedDamage")]
	public PlantStatData TimeUntilOverHeatedDamage;

	[XmlElement("TimeUntilSuffocatedDamage")]
	public PlantStatData TimeUntilSuffocatedDamage;

	[XmlElement("TimeUntilLowPressureDamage")]
	public PlantStatData TimeUntilLowPressureDamage;

	[XmlElement("TimeUntilHighPressureDamage")]
	public PlantStatData TimeUntilHighPressureDamage;

	[XmlElement("TimeUntilLightDamage")]
	public PlantStatData TimeUntilLightDamage;

	[XmlElement("TimeUntilDarknessDamage")]
	public PlantStatData TimeUntilDarknessDamage;

	[XmlElement("LightPerDay")]
	public PlantStatData LightPerDay;

	[XmlElement("DarknessPerDay")]
	public PlantStatData DarknessPerDay;

	[XmlElement("GrowTemperatureC")]
	public MultiPlantStatData GrowTemperatureC;

	[XmlElement("GrowPressure")]
	public MultiPlantStatData GrowPressure;

	public override void Initialize(ModAbout mod)
	{
		DataCollection.Register(this, mod);
	}
}
