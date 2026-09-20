using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;

namespace Assets.Scripts;

public class GlobalGasMixSaveData
{
	[XmlElement("Volume")]
	public DoubleReference Volume;

	[XmlElement("Oxygen")]
	public DoubleReference Oxygen;

	[XmlElement("Nitrogen")]
	public DoubleReference Nitrogen;

	[XmlElement("CarbonDioxide")]
	public DoubleReference CarbonDioxide;

	[XmlElement("Volatiles")]
	public DoubleReference Methane;

	[XmlElement("Pollutant")]
	public DoubleReference Pollutant;

	[XmlElement("Water")]
	public DoubleReference Water;

	[XmlElement("PollutedWater")]
	public DoubleReference PollutedWater;

	[XmlElement("NitrousOxide")]
	public DoubleReference NitrousOxide;

	[XmlElement("LiquidNitrogen")]
	public DoubleReference LiquidNitrogen;

	[XmlElement("LiquidOxygen")]
	public DoubleReference LiquidOxygen;

	[XmlElement("LiquidVolatiles")]
	public DoubleReference LiquidMethane;

	[XmlElement("Steam")]
	public DoubleReference Steam;

	[XmlElement("LiquidCarbonDioxide")]
	public DoubleReference LiquidCarbonDioxide;

	[XmlElement("LiquidPollutant")]
	public DoubleReference LiquidPollutant;

	[XmlElement("LiquidNitrousOxide")]
	public DoubleReference LiquidNitrousOxide;

	[XmlElement("Hydrogen")]
	public DoubleReference Hydrogen;

	[XmlElement("LiquidHydrogen")]
	public DoubleReference LiquidHydrogen;

	[XmlElement("Hydrazine")]
	public DoubleReference Hydrazine;

	[XmlElement("LiquidHydrazine")]
	public DoubleReference LiquidHydrazine;

	[XmlElement("LiquidAlcohol")]
	public DoubleReference LiquidAlcohol;

	[XmlElement("Helium")]
	public DoubleReference Helium;

	[XmlElement("LiquidSodiumChloride")]
	public DoubleReference LiquidSodiumChloride;

	[XmlElement("Silanol")]
	public DoubleReference Silanol;

	[XmlElement("LiquidSilanol")]
	public DoubleReference LiquidSilanol;

	[XmlElement("HydrochloricAcid")]
	public DoubleReference HydrochloricAcid;

	[XmlElement("LiquidHydrochloricAcid")]
	public DoubleReference LiquidHydrochloricAcid;

	[XmlElement("Ozone")]
	public DoubleReference Ozone;

	[XmlElement("LiquidOzone")]
	public DoubleReference LiquidOzone;

	public GlobalGasMixSaveData()
	{
	}

	public static GlobalGasMixSaveData Create(GlobalGasMix globalGasMix)
	{
		return new GlobalGasMixSaveData(globalGasMix);
	}

	private GlobalGasMixSaveData(GlobalGasMix globalGasMix)
	{
		Volume = new DoubleReference(globalGasMix.Volume.ToDouble());
		Oxygen = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Oxygen).ToDouble());
		Nitrogen = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Nitrogen).ToDouble());
		CarbonDioxide = new DoubleReference(globalGasMix.Get(Chemistry.GasType.CarbonDioxide).ToDouble());
		Methane = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Methane).ToDouble());
		Pollutant = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Pollutant).ToDouble());
		Water = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Water).ToDouble());
		PollutedWater = new DoubleReference(globalGasMix.Get(Chemistry.GasType.PollutedWater).ToDouble());
		NitrousOxide = new DoubleReference(globalGasMix.Get(Chemistry.GasType.NitrousOxide).ToDouble());
		LiquidNitrogen = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidNitrogen).ToDouble());
		LiquidOxygen = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidOxygen).ToDouble());
		LiquidMethane = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidMethane).ToDouble());
		Steam = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Steam).ToDouble());
		LiquidCarbonDioxide = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidCarbonDioxide).ToDouble());
		LiquidPollutant = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidPollutant).ToDouble());
		LiquidNitrousOxide = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidNitrousOxide).ToDouble());
		Hydrogen = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Hydrogen).ToDouble());
		LiquidHydrogen = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidHydrogen).ToDouble());
		Hydrazine = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Hydrazine).ToDouble());
		LiquidHydrazine = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidHydrazine).ToDouble());
		LiquidAlcohol = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidAlcohol).ToDouble());
		Helium = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Helium).ToDouble());
		LiquidSodiumChloride = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidSodiumChloride).ToDouble());
		Silanol = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Silanol).ToDouble());
		LiquidSilanol = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidSilanol).ToDouble());
		HydrochloricAcid = new DoubleReference(globalGasMix.Get(Chemistry.GasType.HydrochloricAcid).ToDouble());
		LiquidHydrochloricAcid = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidHydrochloricAcid).ToDouble());
		Ozone = new DoubleReference(globalGasMix.Get(Chemistry.GasType.Ozone).ToDouble());
		LiquidOzone = new DoubleReference(globalGasMix.Get(Chemistry.GasType.LiquidOzone).ToDouble());
	}
}
