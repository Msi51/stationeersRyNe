using System.Xml.Serialization;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Atmospherics;

[XmlRoot]
public class AtmosphereSaveData : ReferencableSaveData
{
	[XmlElement]
	public Vector3 Position;

	[XmlElement]
	public double Oxygen;

	[XmlElement]
	public double Nitrogen;

	[XmlElement]
	public double CarbonDioxide;

	[XmlElement("Volatiles")]
	public double Methane;

	[XmlElement]
	public double Chlorine;

	[XmlElement]
	public double Water;

	[XmlElement]
	public double PollutedWater;

	[XmlElement]
	public double NitrousOxide;

	[XmlElement]
	public double LiquidNitrogen;

	[XmlElement]
	public double LiquidOxygen;

	[XmlElement("LiquidVolatiles")]
	public double LiquidMethane;

	[XmlElement]
	public double Steam;

	[XmlElement]
	public double LiquidCarbonDioxide;

	[XmlElement]
	public double LiquidPollutant;

	[XmlElement]
	public double LiquidNitrousOxide;

	[XmlElement]
	public double Hydrogen;

	[XmlElement]
	public double LiquidHydrogen;

	[XmlElement]
	public double Hydrazine;

	[XmlElement]
	public double LiquidHydrazine;

	[XmlElement]
	public double LiquidAlcohol;

	[XmlElement]
	public double Helium;

	[XmlElement]
	public double LiquidSodiumChloride;

	[XmlElement]
	public double Silanol;

	[XmlElement]
	public double LiquidSilanol;

	[XmlElement]
	public double HydrochloricAcid;

	[XmlElement]
	public double LiquidHydrochloricAcid;

	[XmlElement]
	public double Ozone;

	[XmlElement]
	public double LiquidOzone;

	[XmlElement]
	public double Energy;

	[XmlElement]
	public double Volume;

	[XmlElement]
	public Vector3 Direction;

	[XmlElement]
	public long NetworkReferenceId;

	[XmlElement]
	public long ThingReferenceId;

	[XmlElement]
	public float CleanBurnRate;

	[XmlElement]
	public long MothershipReferenceId;

	public double TotalQuantity => Oxygen + Nitrogen + CarbonDioxide + Methane + Chlorine + Water + PollutedWater + NitrousOxide + LiquidNitrogen + LiquidOxygen + LiquidMethane + Steam + Hydrogen + LiquidHydrogen + Hydrazine + LiquidHydrazine + LiquidAlcohol + Helium + LiquidSodiumChloride + Silanol + LiquidSilanol + HydrochloricAcid + LiquidHydrochloricAcid + Ozone + LiquidOzone;

	public AtmosphereSaveData()
	{
	}

	public AtmosphereSaveData(Atmosphere atmosphere)
	{
		Position = atmosphere.WorldPosition;
		Direction = atmosphere.Direction;
		Oxygen = atmosphere.GasMixture.Oxygen.Quantity.ToDouble();
		Nitrogen = atmosphere.GasMixture.Nitrogen.Quantity.ToDouble();
		CarbonDioxide = atmosphere.GasMixture.CarbonDioxide.Quantity.ToDouble();
		Methane = atmosphere.GasMixture.Methane.Quantity.ToDouble();
		Chlorine = atmosphere.GasMixture.Pollutant.Quantity.ToDouble();
		Water = atmosphere.GasMixture.Water.Quantity.ToDouble();
		PollutedWater = atmosphere.GasMixture.PollutedWater.Quantity.ToDouble();
		NitrousOxide = atmosphere.GasMixture.NitrousOxide.Quantity.ToDouble();
		LiquidNitrogen = atmosphere.GasMixture.LiquidNitrogen.Quantity.ToDouble();
		LiquidOxygen = atmosphere.GasMixture.LiquidOxygen.Quantity.ToDouble();
		LiquidMethane = atmosphere.GasMixture.LiquidMethane.Quantity.ToDouble();
		Steam = atmosphere.GasMixture.Steam.Quantity.ToDouble();
		LiquidCarbonDioxide = atmosphere.GasMixture.LiquidCarbonDioxide.Quantity.ToDouble();
		LiquidPollutant = atmosphere.GasMixture.LiquidPollutant.Quantity.ToDouble();
		LiquidNitrousOxide = atmosphere.GasMixture.LiquidNitrousOxide.Quantity.ToDouble();
		Hydrogen = atmosphere.GasMixture.Hydrogen.Quantity.ToDouble();
		LiquidHydrogen = atmosphere.GasMixture.LiquidHydrogen.Quantity.ToDouble();
		Hydrazine = atmosphere.GasMixture.Hydrazine.Quantity.ToDouble();
		LiquidHydrazine = atmosphere.GasMixture.LiquidHydrazine.Quantity.ToDouble();
		LiquidAlcohol = atmosphere.GasMixture.LiquidAlcohol.Quantity.ToDouble();
		Helium = atmosphere.GasMixture.Helium.Quantity.ToDouble();
		LiquidSodiumChloride = atmosphere.GasMixture.LiquidSodiumChloride.Quantity.ToDouble();
		Silanol = atmosphere.GasMixture.Silanol.Quantity.ToDouble();
		LiquidSilanol = atmosphere.GasMixture.LiquidSilanol.Quantity.ToDouble();
		HydrochloricAcid = atmosphere.GasMixture.HydrochloricAcid.Quantity.ToDouble();
		LiquidHydrochloricAcid = atmosphere.GasMixture.LiquidHydrochloricAcid.Quantity.ToDouble();
		Ozone = atmosphere.GasMixture.Ozone.Quantity.ToDouble();
		LiquidOzone = atmosphere.GasMixture.LiquidOzone.Quantity.ToDouble();
		Volume = atmosphere.Volume.ToDouble();
		Energy = atmosphere.GasMixture.TotalEnergy.ToDouble();
		ReferenceId = atmosphere.ReferenceId;
		NetworkReferenceId = atmosphere.AtmosphericsNetwork?.ReferenceId ?? 0;
		ThingReferenceId = (atmosphere.Thing ? atmosphere.Thing.ReferenceId : 0);
		MothershipReferenceId = 0L;
		CleanBurnRate = atmosphere.CleanBurnRate;
	}
}
