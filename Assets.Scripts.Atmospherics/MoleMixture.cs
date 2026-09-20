using System.Xml.Serialization;

namespace Assets.Scripts.Atmospherics;

public struct MoleMixture
{
	public double Oxygen;

	public double Nitrogen;

	public double CarbonDioxide;

	[XmlElement("Volatiles")]
	public double Methane;

	public double Pollutant;

	public double Water;

	public double PollutedWater;

	public double NitrousOxide;

	public double LiquidNitrogen;

	public double LiquidOxygen;

	[XmlElement("LiquidVolatiles")]
	public double LiquidMethane;

	public double Steam;

	public double LiquidCarbonDioxide;

	public double LiquidPollutant;

	public double LiquidNitrousOxide;

	public double Hydrogen;

	public double LiquidHydrogen;

	public double Hydrazine;

	public double LiquidHydrazine;

	public double LiquidAlcohol;

	public double Helium;

	public double LiquidSodiumChloride;

	public double Silanol;

	public double LiquidSilanol;

	public double HydrochloricAcid;

	public double LiquidHydrochloricAcid;

	public double Ozone;

	public double LiquidOzone;

	[XmlAttribute("Rule")]
	public MixRule Rule;

	public bool IsAny
	{
		get
		{
			if (!double.IsNaN(Oxygen))
			{
				return true;
			}
			if (!double.IsNaN(Nitrogen))
			{
				return true;
			}
			if (!double.IsNaN(CarbonDioxide))
			{
				return true;
			}
			if (!double.IsNaN(Methane))
			{
				return true;
			}
			if (!double.IsNaN(Pollutant))
			{
				return true;
			}
			if (!double.IsNaN(Water))
			{
				return true;
			}
			if (!double.IsNaN(PollutedWater))
			{
				return true;
			}
			if (!double.IsNaN(NitrousOxide))
			{
				return true;
			}
			if (!double.IsNaN(LiquidNitrogen))
			{
				return true;
			}
			if (!double.IsNaN(LiquidOxygen))
			{
				return true;
			}
			if (!double.IsNaN(LiquidMethane))
			{
				return true;
			}
			if (!double.IsNaN(Steam))
			{
				return true;
			}
			if (!double.IsNaN(LiquidCarbonDioxide))
			{
				return true;
			}
			if (!double.IsNaN(LiquidPollutant))
			{
				return true;
			}
			if (!double.IsNaN(LiquidNitrousOxide))
			{
				return true;
			}
			if (!double.IsNaN(Hydrogen))
			{
				return true;
			}
			if (!double.IsNaN(LiquidHydrogen))
			{
				return true;
			}
			if (!double.IsNaN(Hydrazine))
			{
				return true;
			}
			if (!double.IsNaN(LiquidHydrazine))
			{
				return true;
			}
			if (!double.IsNaN(LiquidAlcohol))
			{
				return true;
			}
			if (!double.IsNaN(Helium))
			{
				return true;
			}
			if (!double.IsNaN(LiquidSodiumChloride))
			{
				return true;
			}
			if (!double.IsNaN(Silanol))
			{
				return true;
			}
			if (!double.IsNaN(LiquidSilanol))
			{
				return true;
			}
			if (!double.IsNaN(HydrochloricAcid))
			{
				return true;
			}
			if (!double.IsNaN(LiquidHydrochloricAcid))
			{
				return true;
			}
			if (!double.IsNaN(Ozone))
			{
				return true;
			}
			if (!double.IsNaN(LiquidOzone))
			{
				return true;
			}
			return false;
		}
	}

	public bool IsAnyToRemove
	{
		get
		{
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Oxygen)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Nitrogen)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(CarbonDioxide)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Methane)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Pollutant)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Water)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(PollutedWater)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(NitrousOxide)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidNitrogen)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidOxygen)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidMethane)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Steam)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidCarbonDioxide)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidPollutant)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidNitrousOxide)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Hydrogen)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidHydrogen)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Hydrazine)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidHydrazine)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidAlcohol)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Helium)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidSodiumChloride)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Silanol)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidSilanol)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(HydrochloricAcid)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidHydrochloricAcid)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(Ozone)))
			{
				return true;
			}
			if (!Chemistry.IsMolecularlyZero(new MoleQuantity(LiquidOzone)))
			{
				return true;
			}
			return false;
		}
	}

	public MoleMixture(GasMixture inputMixture)
	{
		Oxygen = inputMixture.Oxygen.Quantity.ToDouble();
		Nitrogen = inputMixture.Nitrogen.Quantity.ToDouble();
		CarbonDioxide = inputMixture.CarbonDioxide.Quantity.ToDouble();
		Methane = inputMixture.Methane.Quantity.ToDouble();
		Pollutant = inputMixture.Pollutant.Quantity.ToDouble();
		Water = inputMixture.Water.Quantity.ToDouble();
		PollutedWater = inputMixture.PollutedWater.Quantity.ToDouble();
		NitrousOxide = inputMixture.NitrousOxide.Quantity.ToDouble();
		LiquidNitrogen = inputMixture.LiquidNitrogen.Quantity.ToDouble();
		LiquidOxygen = inputMixture.LiquidOxygen.Quantity.ToDouble();
		LiquidMethane = inputMixture.LiquidMethane.Quantity.ToDouble();
		Steam = inputMixture.Steam.Quantity.ToDouble();
		LiquidCarbonDioxide = inputMixture.LiquidCarbonDioxide.Quantity.ToDouble();
		LiquidPollutant = inputMixture.LiquidPollutant.Quantity.ToDouble();
		LiquidNitrousOxide = inputMixture.LiquidNitrousOxide.Quantity.ToDouble();
		Hydrogen = inputMixture.Hydrogen.Quantity.ToDouble();
		LiquidHydrogen = inputMixture.LiquidHydrogen.Quantity.ToDouble();
		Hydrazine = inputMixture.Hydrazine.Quantity.ToDouble();
		LiquidHydrazine = inputMixture.LiquidHydrazine.Quantity.ToDouble();
		LiquidAlcohol = inputMixture.LiquidAlcohol.Quantity.ToDouble();
		Helium = inputMixture.Helium.Quantity.ToDouble();
		LiquidSodiumChloride = inputMixture.LiquidSodiumChloride.Quantity.ToDouble();
		Silanol = inputMixture.Silanol.Quantity.ToDouble();
		LiquidSilanol = inputMixture.LiquidSilanol.Quantity.ToDouble();
		HydrochloricAcid = inputMixture.HydrochloricAcid.Quantity.ToDouble();
		LiquidHydrochloricAcid = inputMixture.LiquidHydrochloricAcid.Quantity.ToDouble();
		Ozone = inputMixture.Ozone.Quantity.ToDouble();
		LiquidOzone = inputMixture.LiquidOzone.Quantity.ToDouble();
		Rule = MixRule.None;
	}

	private MoleMixture(MoleMixture inputMixture, float right)
	{
		Oxygen = inputMixture.Oxygen * (double)right;
		Nitrogen = inputMixture.Nitrogen * (double)right;
		CarbonDioxide = inputMixture.CarbonDioxide * (double)right;
		Methane = inputMixture.Methane * (double)right;
		Pollutant = inputMixture.Pollutant * (double)right;
		Water = inputMixture.Water * (double)right;
		PollutedWater = inputMixture.PollutedWater * (double)right;
		NitrousOxide = inputMixture.NitrousOxide * (double)right;
		LiquidNitrogen = inputMixture.LiquidNitrogen * (double)right;
		LiquidOxygen = inputMixture.LiquidOxygen * (double)right;
		LiquidMethane = inputMixture.LiquidMethane * (double)right;
		Steam = inputMixture.Steam * (double)right;
		LiquidCarbonDioxide = inputMixture.LiquidCarbonDioxide * (double)right;
		LiquidPollutant = inputMixture.LiquidPollutant * (double)right;
		LiquidNitrousOxide = inputMixture.LiquidNitrousOxide * (double)right;
		Hydrogen = inputMixture.Hydrogen * (double)right;
		LiquidHydrogen = inputMixture.LiquidHydrogen * (double)right;
		Hydrazine = inputMixture.Hydrazine * (double)right;
		LiquidHydrazine = inputMixture.LiquidHydrazine * (double)right;
		LiquidAlcohol = inputMixture.LiquidAlcohol * (double)right;
		Helium = inputMixture.Helium * (double)right;
		LiquidSodiumChloride = inputMixture.LiquidSodiumChloride * (double)right;
		Silanol = inputMixture.Silanol * (double)right;
		LiquidSilanol = inputMixture.LiquidSilanol * (double)right;
		HydrochloricAcid = inputMixture.HydrochloricAcid * (double)right;
		LiquidHydrochloricAcid = inputMixture.LiquidHydrochloricAcid * (double)right;
		Ozone = inputMixture.Ozone * (double)right;
		LiquidOzone = inputMixture.LiquidOzone * (double)right;
		Rule = inputMixture.Rule;
	}

	public bool Contains(MoleMixture mix)
	{
		if (!IsValid(Oxygen, mix.Oxygen))
		{
			return false;
		}
		if (!IsValid(Nitrogen, mix.Nitrogen))
		{
			return false;
		}
		if (!IsValid(CarbonDioxide, mix.CarbonDioxide))
		{
			return false;
		}
		if (!IsValid(Methane, mix.Methane))
		{
			return false;
		}
		if (!IsValid(Pollutant, mix.Pollutant))
		{
			return false;
		}
		if (!IsValid(Water, mix.Water))
		{
			return false;
		}
		if (!IsValid(PollutedWater, mix.PollutedWater))
		{
			return false;
		}
		if (!IsValid(NitrousOxide, mix.NitrousOxide))
		{
			return false;
		}
		if (!IsValid(LiquidNitrogen, mix.LiquidNitrogen))
		{
			return false;
		}
		if (!IsValid(LiquidOxygen, mix.LiquidOxygen))
		{
			return false;
		}
		if (!IsValid(LiquidMethane, mix.LiquidMethane))
		{
			return false;
		}
		if (!IsValid(Steam, mix.Steam))
		{
			return false;
		}
		if (!IsValid(LiquidCarbonDioxide, mix.LiquidCarbonDioxide))
		{
			return false;
		}
		if (!IsValid(LiquidPollutant, mix.LiquidPollutant))
		{
			return false;
		}
		if (!IsValid(LiquidNitrousOxide, mix.LiquidNitrousOxide))
		{
			return false;
		}
		if (!IsValid(Hydrogen, mix.Hydrogen))
		{
			return false;
		}
		if (!IsValid(LiquidHydrogen, mix.LiquidHydrogen))
		{
			return false;
		}
		if (!IsValid(Hydrazine, mix.Hydrazine))
		{
			return false;
		}
		if (!IsValid(LiquidHydrazine, mix.LiquidHydrazine))
		{
			return false;
		}
		if (!IsValid(LiquidAlcohol, mix.LiquidAlcohol))
		{
			return false;
		}
		if (!IsValid(Helium, mix.Helium))
		{
			return false;
		}
		if (!IsValid(LiquidSodiumChloride, mix.LiquidSodiumChloride))
		{
			return false;
		}
		if (!IsValid(Silanol, mix.Silanol))
		{
			return false;
		}
		if (!IsValid(LiquidSilanol, mix.LiquidSilanol))
		{
			return false;
		}
		if (!IsValid(HydrochloricAcid, mix.HydrochloricAcid))
		{
			return false;
		}
		if (!IsValid(LiquidHydrochloricAcid, mix.LiquidHydrochloricAcid))
		{
			return false;
		}
		if (!IsValid(Ozone, mix.Ozone))
		{
			return false;
		}
		if (!IsValid(LiquidOzone, mix.LiquidOzone))
		{
			return false;
		}
		return true;
	}

	private bool IsValid(double required, double moles)
	{
		if (Rule == MixRule.Pure)
		{
			if (double.IsNaN(required) && !Chemistry.IsMolecularlyZero(new MoleQuantity(moles)))
			{
				return false;
			}
			if (Chemistry.IsMolecularlyZero(new MoleQuantity(required)) && !Chemistry.IsMolecularlyZero(new MoleQuantity(moles)))
			{
				return false;
			}
			if (required > 0.0 && moles < required)
			{
				return false;
			}
			return true;
		}
		if (double.IsNaN(required))
		{
			return true;
		}
		if (required > 0.0 && moles < required)
		{
			return false;
		}
		return true;
	}

	public GasMixture ToGasMixture()
	{
		return new GasMixture(this);
	}

	public static MoleMixture operator *(MoleMixture left, float right)
	{
		return new MoleMixture(left, right);
	}
}
