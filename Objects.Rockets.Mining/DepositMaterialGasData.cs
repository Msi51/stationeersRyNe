using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;

namespace Objects.Rockets.Mining;

public class DepositMaterialGasData
{
	[XmlAttribute("Weight")]
	public float Weight = 1f;

	[XmlAttribute("TemperatureC")]
	public float TemperatureC = float.NaN;

	[XmlAttribute("TemperatureK")]
	public float TemperatureK = float.NaN;

	[XmlAttribute("Oxygen")]
	public float Oxygen;

	[XmlAttribute("Nitrogen")]
	public float Nitrogen;

	[XmlAttribute("CarbonDioxide")]
	public float CarbonDioxide;

	[XmlAttribute("Volatiles")]
	public float Methane;

	[XmlAttribute("Pollutant")]
	public float Pollutant;

	[XmlAttribute("Water")]
	public float Water;

	[XmlAttribute("PollutedWater")]
	public float PollutedWater;

	[XmlAttribute("NitrousOxide")]
	public float NitrousOxide;

	[XmlAttribute("LiquidNitrogen")]
	public float LiquidNitrogen;

	[XmlAttribute("LiquidOxygen")]
	public float LiquidOxygen;

	[XmlAttribute("LiquidVolatiles")]
	public float LiquidMethane;

	[XmlAttribute("Steam")]
	public float Steam;

	[XmlAttribute("LiquidCarbonDioxide")]
	public float LiquidCarbonDioxide;

	[XmlAttribute("LiquidPollutant")]
	public float LiquidPollutant;

	[XmlAttribute("LiquidNitrousOxide")]
	public float LiquidNitrousOxide;

	[XmlAttribute("Hydrogen")]
	public float Hydrogen;

	[XmlAttribute("LiquidHydrogen")]
	public float LiquidHydrogen;

	[XmlAttribute("Hydrazine")]
	public float Hydrazine;

	[XmlAttribute("LiquidHydrazine")]
	public float LiquidHydrazine;

	[XmlAttribute("LiquidAlcohol")]
	public float LiquidAlcohol;

	[XmlAttribute("Helium")]
	public float Helium;

	[XmlAttribute("LiquidSodiumChloride")]
	public float LiquidSodiumChloride;

	[XmlAttribute("Silanol")]
	public float Silanol;

	[XmlAttribute("LiquidSilanol")]
	public float LiquidSilanol;

	[XmlAttribute("HydrochloricAcid")]
	public float HydrochloricAcid;

	[XmlAttribute("LiquidHydrochloricAcid")]
	public float LiquidHydrochloricAcid;

	[XmlAttribute("Ozone")]
	public float Ozone;

	[XmlAttribute("LiquidOzone")]
	public float LiquidOzone;

	[XmlIgnore]
	private bool _isCached;

	[XmlIgnore]
	public readonly List<SpawnGas> SpawnGases = new List<SpawnGas>();

	[XmlIgnore]
	public TemperatureKelvin Temperature = Chemistry.Temperature.ZeroDegrees;

	[XmlIgnore]
	public PressurekPa Pressure = PressurekPa.One;

	public void Cache()
	{
		if (_isCached)
		{
			return;
		}
		_isCached = true;
		SpawnGases.Clear();
		if (!float.IsNaN(TemperatureK))
		{
			Temperature = new TemperatureKelvin(TemperatureK);
		}
		if (!float.IsNaN(TemperatureC))
		{
			Temperature = RocketMath.CelsiusToKelvin(TemperatureC);
		}
		Populate(SpawnGases);
		MoleQuantity quantity = new MoleQuantity(0.0);
		MoleEnergy zero = MoleEnergy.Zero;
		foreach (SpawnGas spawnGase in SpawnGases)
		{
			quantity += spawnGase.GetQuantity();
			zero += spawnGase.GetEnergy();
		}
		Pressure = IdealGas.Pressure(quantity, Temperature, new VolumeLitres(200.0));
	}

	public void AppendToString(ref StringBuilder sb, bool asAtmosphere)
	{
		if (sb == null)
		{
			return;
		}
		if (asAtmosphere)
		{
			AtmosphericsManager.DisplayBasicAtmosphere(Temperature, Pressure, sb, Pipe.ContentType.All, indent: false, includeCelsius: false);
			sb.AppendLine();
		}
		foreach (SpawnGas spawnGase in SpawnGases)
		{
			sb.AppendLine(spawnGase.ToString());
		}
	}

	public void Populate(List<SpawnGas> spawnGasses)
	{
		if (Oxygen > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Oxygen, Oxygen, Temperature));
		}
		if (Nitrogen > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Nitrogen, Nitrogen, Temperature));
		}
		if (CarbonDioxide > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.CarbonDioxide, CarbonDioxide, Temperature));
		}
		if (Methane > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Methane, Methane, Temperature));
		}
		if (Pollutant > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Pollutant, Pollutant, Temperature));
		}
		if (Water > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Water, Water, Temperature));
		}
		if (PollutedWater > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.PollutedWater, PollutedWater, Temperature));
		}
		if (NitrousOxide > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.NitrousOxide, NitrousOxide, Temperature));
		}
		if (LiquidNitrogen > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidNitrogen, LiquidNitrogen, Temperature));
		}
		if (LiquidOxygen > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidOxygen, LiquidOxygen, Temperature));
		}
		if (LiquidMethane > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidMethane, LiquidMethane, Temperature));
		}
		if (Steam > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Steam, Steam, Temperature));
		}
		if (LiquidCarbonDioxide > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidCarbonDioxide, LiquidCarbonDioxide, Temperature));
		}
		if (LiquidPollutant > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidPollutant, LiquidPollutant, Temperature));
		}
		if (LiquidNitrousOxide > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidNitrousOxide, LiquidNitrousOxide, Temperature));
		}
		if (Hydrogen > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Hydrogen, Hydrogen, Temperature));
		}
		if (LiquidHydrogen > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidHydrogen, LiquidHydrogen, Temperature));
		}
		if (Hydrazine > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Hydrazine, Hydrazine, Temperature));
		}
		if (LiquidHydrazine > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidHydrazine, LiquidHydrazine, Temperature));
		}
		if (LiquidAlcohol > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidAlcohol, LiquidAlcohol, Temperature));
		}
		if (Helium > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Helium, Helium, Temperature));
		}
		if (LiquidSodiumChloride > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidSodiumChloride, LiquidSodiumChloride, Temperature));
		}
		if (Silanol > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Silanol, Silanol, Temperature));
		}
		if (LiquidSilanol > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidSilanol, LiquidSilanol, Temperature));
		}
		if (HydrochloricAcid > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.HydrochloricAcid, HydrochloricAcid, Temperature));
		}
		if (LiquidHydrochloricAcid > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidHydrochloricAcid, LiquidHydrochloricAcid, Temperature));
		}
		if (Ozone > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.Ozone, Ozone, Temperature));
		}
		if (LiquidOzone > 0f)
		{
			spawnGasses.Add(new SpawnGas(Chemistry.GasType.LiquidOzone, LiquidOzone, Temperature));
		}
	}

	public TemperatureKelvin GetTemperature()
	{
		return Temperature;
	}

	public PressurekPa GetPressure()
	{
		return Pressure;
	}
}
