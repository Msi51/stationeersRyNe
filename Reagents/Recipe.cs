using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using UnityEngine;

namespace Reagents;

[XmlRoot]
public struct Recipe : IEquatable<Recipe>
{
	public static Recipe INVALID;

	public double Flour;

	public double Milk;

	public double Egg;

	public double Iron;

	public double Gold;

	public double Carbon;

	public double Uranium;

	public double Copper;

	public double Steel;

	public double Hydrocarbon;

	public double Silver;

	public double Nickel;

	public double Lead;

	public double Electrum;

	public double Invar;

	public double Constantan;

	public double Solder;

	public double Plastic;

	public double Silicon;

	public double SalicylicAcid;

	public double Alcohol;

	public double Oil;

	public double Potato;

	public double Tomato;

	public double Fenoxitone;

	public double ColorRed;

	public double ColorGreen;

	public double ColorBlue;

	public double ColorYellow;

	public double ColorOrange;

	public double Pumpkin;

	public double Rice;

	public double Waspaloy;

	public double Stellite;

	public double Inconel;

	public double Hastelloy;

	public double Astroloy;

	public double Cobalt;

	public double Corn;

	public double Wheat;

	public double Biomass;

	public double Soy;

	public double Mushroom;

	public double Sugar;

	public double Cocoa;

	public double Cheese;

	public float Time;

	public float Energy;

	public Temperature Temperature;

	public Pressure Pressure;

	public MoleMixture RequiredMix;

	private static StringBuilder _reagentSb;

	public int CountTypes
	{
		get
		{
			int num = 0;
			if (Flour > 0.0)
			{
				num++;
			}
			if (Milk > 0.0)
			{
				num++;
			}
			if (Egg > 0.0)
			{
				num++;
			}
			if (Iron > 0.0)
			{
				num++;
			}
			if (Gold > 0.0)
			{
				num++;
			}
			if (Carbon > 0.0)
			{
				num++;
			}
			if (Uranium > 0.0)
			{
				num++;
			}
			if (Copper > 0.0)
			{
				num++;
			}
			if (Steel > 0.0)
			{
				num++;
			}
			if (Hydrocarbon > 0.0)
			{
				num++;
			}
			if (Silver > 0.0)
			{
				num++;
			}
			if (Nickel > 0.0)
			{
				num++;
			}
			if (Lead > 0.0)
			{
				num++;
			}
			if (Electrum > 0.0)
			{
				num++;
			}
			if (Invar > 0.0)
			{
				num++;
			}
			if (Constantan > 0.0)
			{
				num++;
			}
			if (Solder > 0.0)
			{
				num++;
			}
			if (Plastic > 0.0)
			{
				num++;
			}
			if (Silicon > 0.0)
			{
				num++;
			}
			if (SalicylicAcid > 0.0)
			{
				num++;
			}
			if (Alcohol > 0.0)
			{
				num++;
			}
			if (Oil > 0.0)
			{
				num++;
			}
			if (Potato > 0.0)
			{
				num++;
			}
			if (Tomato > 0.0)
			{
				num++;
			}
			if (Fenoxitone > 0.0)
			{
				num++;
			}
			if (ColorRed > 0.0)
			{
				num++;
			}
			if (ColorGreen > 0.0)
			{
				num++;
			}
			if (ColorBlue > 0.0)
			{
				num++;
			}
			if (ColorYellow > 0.0)
			{
				num++;
			}
			if (ColorOrange > 0.0)
			{
				num++;
			}
			if (Pumpkin > 0.0)
			{
				num++;
			}
			if (Rice > 0.0)
			{
				num++;
			}
			if (Waspaloy > 0.0)
			{
				num++;
			}
			if (Stellite > 0.0)
			{
				num++;
			}
			if (Inconel > 0.0)
			{
				num++;
			}
			if (Hastelloy > 0.0)
			{
				num++;
			}
			if (Astroloy > 0.0)
			{
				num++;
			}
			if (Cobalt > 0.0)
			{
				num++;
			}
			if (Corn > 0.0)
			{
				num++;
			}
			if (Wheat > 0.0)
			{
				num++;
			}
			if (Biomass > 0.0)
			{
				num++;
			}
			if (Soy > 0.0)
			{
				num++;
			}
			if (Mushroom > 0.0)
			{
				num++;
			}
			if (Sugar > 0.0)
			{
				num++;
			}
			if (Cocoa > 0.0)
			{
				num++;
			}
			if (Cheese > 0.0)
			{
				num++;
			}
			return num;
		}
	}

	public void Check()
	{
		Temperature.Check();
		Pressure.Check();
	}

	public Recipe(double value)
	{
		Flour = value;
		Milk = value;
		Egg = value;
		Iron = value;
		Gold = value;
		Carbon = value;
		Uranium = value;
		Copper = value;
		Steel = value;
		Hydrocarbon = value;
		Silver = value;
		Nickel = value;
		Lead = value;
		Electrum = value;
		Invar = value;
		Constantan = value;
		Solder = value;
		Plastic = value;
		Silicon = value;
		SalicylicAcid = value;
		Alcohol = value;
		Oil = value;
		Potato = value;
		Tomato = value;
		Fenoxitone = value;
		ColorRed = value;
		ColorGreen = value;
		ColorBlue = value;
		ColorYellow = value;
		ColorOrange = value;
		Pumpkin = value;
		Rice = value;
		Waspaloy = value;
		Stellite = value;
		Inconel = value;
		Hastelloy = value;
		Astroloy = value;
		Cobalt = value;
		Corn = value;
		Wheat = value;
		Biomass = value;
		Soy = value;
		Mushroom = value;
		Sugar = value;
		Cocoa = value;
		Cheese = value;
		Time = float.NaN;
		Energy = float.NaN;
		Temperature = new Temperature(new TemperatureKelvin(double.NaN), TemperatureKelvin.Zero);
		Pressure = new Pressure(new PressurekPa(double.NaN), PressurekPa.Zero);
		RequiredMix = default(MoleMixture);
	}

	public Recipe(double flour = 0.0, double milk = 0.0, double egg = 0.0, double iron = 0.0, double gold = 0.0, double carbon = 0.0, double uranium = 0.0, double copper = 0.0, double steel = 0.0, double hydrocarbon = 0.0, double silver = 0.0, double nickel = 0.0, double lead = 0.0, double electrum = 0.0, double invar = 0.0, double constantan = 0.0, double solder = 0.0, double plastic = 0.0, double silicon = 0.0, double salicylicacid = 0.0, double alcohol = 0.0, double oil = 0.0, double potato = 0.0, double tomato = 0.0, double fenoxitone = 0.0, double colorred = 0.0, double colorgreen = 0.0, double colorblue = 0.0, double coloryellow = 0.0, double colororange = 0.0, double pumpkin = 0.0, double rice = 0.0, double waspaloy = 0.0, double stellite = 0.0, double inconel = 0.0, double hastelloy = 0.0, double astroloy = 0.0, double cobalt = 0.0, double corn = 0.0, double wheat = 0.0, double biomass = 0.0, double soy = 0.0, double mushroom = 0.0, double sugar = 0.0, double cocoa = 0.0, Temperature temperature = default(Temperature), Pressure pressure = default(Pressure), MoleMixture requiredMix = default(MoleMixture), float time = 0f, float energy = 0f, float cheese = 0f)
	{
		Flour = flour;
		Milk = milk;
		Egg = egg;
		Iron = iron;
		Gold = gold;
		Carbon = carbon;
		Uranium = uranium;
		Copper = copper;
		Steel = steel;
		Hydrocarbon = hydrocarbon;
		Silver = silver;
		Nickel = nickel;
		Lead = lead;
		Electrum = electrum;
		Invar = invar;
		Constantan = constantan;
		Solder = solder;
		Plastic = plastic;
		Silicon = silicon;
		SalicylicAcid = salicylicacid;
		Alcohol = alcohol;
		Oil = oil;
		Potato = potato;
		Tomato = tomato;
		Fenoxitone = fenoxitone;
		ColorRed = colorred;
		ColorGreen = colorgreen;
		ColorBlue = colorblue;
		ColorYellow = coloryellow;
		ColorOrange = colororange;
		Pumpkin = pumpkin;
		Rice = rice;
		Waspaloy = waspaloy;
		Stellite = stellite;
		Inconel = inconel;
		Hastelloy = hastelloy;
		Astroloy = astroloy;
		Cobalt = cobalt;
		Corn = corn;
		Wheat = wheat;
		Biomass = biomass;
		Soy = soy;
		Mushroom = mushroom;
		Sugar = sugar;
		Cocoa = cocoa;
		Cheese = cheese;
		Temperature = temperature;
		Pressure = pressure;
		Time = time;
		Energy = energy;
		RequiredMix = requiredMix;
	}

	public Recipe(ReagentMixture reagentMixture, Atmosphere atmosphere)
	{
		Flour = reagentMixture.Flour.Quantity;
		Milk = reagentMixture.Milk.Quantity;
		Egg = reagentMixture.Egg.Quantity;
		Iron = reagentMixture.Iron.Quantity;
		Gold = reagentMixture.Gold.Quantity;
		Carbon = reagentMixture.Carbon.Quantity;
		Uranium = reagentMixture.Uranium.Quantity;
		Copper = reagentMixture.Copper.Quantity;
		Steel = reagentMixture.Steel.Quantity;
		Hydrocarbon = reagentMixture.Hydrocarbon.Quantity;
		Silver = reagentMixture.Silver.Quantity;
		Nickel = reagentMixture.Nickel.Quantity;
		Lead = reagentMixture.Lead.Quantity;
		Electrum = reagentMixture.Electrum.Quantity;
		Invar = reagentMixture.Invar.Quantity;
		Constantan = reagentMixture.Constantan.Quantity;
		Solder = reagentMixture.Solder.Quantity;
		Plastic = reagentMixture.Plastic.Quantity;
		Silicon = reagentMixture.Silicon.Quantity;
		SalicylicAcid = reagentMixture.SalicylicAcid.Quantity;
		Alcohol = reagentMixture.Alcohol.Quantity;
		Oil = reagentMixture.Oil.Quantity;
		Potato = reagentMixture.Potato.Quantity;
		Tomato = reagentMixture.Tomato.Quantity;
		Fenoxitone = reagentMixture.Fenoxitone.Quantity;
		ColorRed = reagentMixture.ColorRed.Quantity;
		ColorGreen = reagentMixture.ColorGreen.Quantity;
		ColorBlue = reagentMixture.ColorBlue.Quantity;
		ColorYellow = reagentMixture.ColorYellow.Quantity;
		ColorOrange = reagentMixture.ColorOrange.Quantity;
		Pumpkin = reagentMixture.Pumpkin.Quantity;
		Rice = reagentMixture.Rice.Quantity;
		Waspaloy = reagentMixture.Waspaloy.Quantity;
		Stellite = reagentMixture.Stellite.Quantity;
		Inconel = reagentMixture.Inconel.Quantity;
		Hastelloy = reagentMixture.Hastelloy.Quantity;
		Astroloy = reagentMixture.Astroloy.Quantity;
		Cobalt = reagentMixture.Cobalt.Quantity;
		Corn = reagentMixture.Corn.Quantity;
		Wheat = reagentMixture.Wheat.Quantity;
		Biomass = reagentMixture.Biomass.Quantity;
		Soy = reagentMixture.Soy.Quantity;
		Mushroom = reagentMixture.Mushroom.Quantity;
		Sugar = reagentMixture.Sugar.Quantity;
		Cocoa = reagentMixture.Cocoa.Quantity;
		Cheese = reagentMixture.Cheese.Quantity;
		if (atmosphere != null)
		{
			Temperature = new Temperature(atmosphere.Temperature, atmosphere.Temperature);
			Pressure = new Pressure(atmosphere.PressureGassesAndLiquids, atmosphere.PressureGassesAndLiquids);
			RequiredMix = new MoleMixture(atmosphere.GasMixture);
		}
		else
		{
			Temperature = new Temperature(Chemistry.Temperature.ZeroDegrees, Chemistry.Temperature.ZeroDegrees);
			Pressure = new Pressure(Chemistry.OneAtmosphere, Chemistry.OneAtmosphere);
			RequiredMix = default(MoleMixture);
		}
		Time = 0f;
		Energy = 0f;
	}

	public Recipe(ReagentMixture reagentMixture, float temperature, float pressure)
	{
		Flour = reagentMixture.Flour.Quantity;
		Milk = reagentMixture.Milk.Quantity;
		Egg = reagentMixture.Egg.Quantity;
		Iron = reagentMixture.Iron.Quantity;
		Gold = reagentMixture.Gold.Quantity;
		Carbon = reagentMixture.Carbon.Quantity;
		Uranium = reagentMixture.Uranium.Quantity;
		Copper = reagentMixture.Copper.Quantity;
		Steel = reagentMixture.Steel.Quantity;
		Hydrocarbon = reagentMixture.Hydrocarbon.Quantity;
		Silver = reagentMixture.Silver.Quantity;
		Nickel = reagentMixture.Nickel.Quantity;
		Lead = reagentMixture.Lead.Quantity;
		Electrum = reagentMixture.Electrum.Quantity;
		Invar = reagentMixture.Invar.Quantity;
		Constantan = reagentMixture.Constantan.Quantity;
		Solder = reagentMixture.Solder.Quantity;
		Plastic = reagentMixture.Plastic.Quantity;
		Silicon = reagentMixture.Silicon.Quantity;
		SalicylicAcid = reagentMixture.SalicylicAcid.Quantity;
		Alcohol = reagentMixture.Alcohol.Quantity;
		Oil = reagentMixture.Oil.Quantity;
		Potato = reagentMixture.Potato.Quantity;
		Tomato = reagentMixture.Tomato.Quantity;
		Fenoxitone = reagentMixture.Fenoxitone.Quantity;
		ColorRed = reagentMixture.ColorRed.Quantity;
		ColorGreen = reagentMixture.ColorGreen.Quantity;
		ColorBlue = reagentMixture.ColorBlue.Quantity;
		ColorYellow = reagentMixture.ColorYellow.Quantity;
		ColorOrange = reagentMixture.ColorOrange.Quantity;
		Pumpkin = reagentMixture.Pumpkin.Quantity;
		Rice = reagentMixture.Rice.Quantity;
		Waspaloy = reagentMixture.Waspaloy.Quantity;
		Stellite = reagentMixture.Stellite.Quantity;
		Inconel = reagentMixture.Inconel.Quantity;
		Hastelloy = reagentMixture.Hastelloy.Quantity;
		Astroloy = reagentMixture.Astroloy.Quantity;
		Cobalt = reagentMixture.Cobalt.Quantity;
		Corn = reagentMixture.Corn.Quantity;
		Wheat = reagentMixture.Wheat.Quantity;
		Biomass = reagentMixture.Biomass.Quantity;
		Soy = reagentMixture.Soy.Quantity;
		Mushroom = reagentMixture.Mushroom.Quantity;
		Sugar = reagentMixture.Sugar.Quantity;
		Cocoa = reagentMixture.Cocoa.Quantity;
		Cheese = reagentMixture.Cheese.Quantity;
		Temperature = new Temperature(new TemperatureKelvin(temperature), new TemperatureKelvin(temperature));
		Pressure = new Pressure(new PressurekPa(pressure), new PressurekPa(pressure));
		RequiredMix = default(MoleMixture);
		Time = 0f;
		Energy = 0f;
	}

	public bool Equals(Recipe other)
	{
		if (Flour.Equals(other.Flour) && Milk.Equals(other.Milk) && Egg.Equals(other.Egg) && Iron.Equals(other.Iron) && Gold.Equals(other.Gold) && Carbon.Equals(other.Carbon) && Uranium.Equals(other.Uranium) && Copper.Equals(other.Copper) && Steel.Equals(other.Steel) && Hydrocarbon.Equals(other.Hydrocarbon) && Silver.Equals(other.Silver) && Nickel.Equals(other.Nickel) && Lead.Equals(other.Lead) && Electrum.Equals(other.Electrum) && Invar.Equals(other.Invar) && Constantan.Equals(other.Constantan) && Solder.Equals(other.Solder) && Plastic.Equals(other.Plastic) && Silicon.Equals(other.Silicon) && SalicylicAcid.Equals(other.SalicylicAcid) && Alcohol.Equals(other.Alcohol) && Oil.Equals(other.Oil) && Potato.Equals(other.Potato) && Tomato.Equals(other.Tomato) && Fenoxitone.Equals(other.Fenoxitone) && ColorRed.Equals(other.ColorRed) && ColorGreen.Equals(other.ColorGreen) && ColorBlue.Equals(other.ColorBlue) && ColorYellow.Equals(other.ColorYellow) && ColorOrange.Equals(other.ColorOrange) && Pumpkin.Equals(other.Pumpkin) && Rice.Equals(other.Rice) && Waspaloy.Equals(other.Waspaloy) && Stellite.Equals(other.Stellite) && Inconel.Equals(other.Inconel) && Hastelloy.Equals(other.Hastelloy) && Astroloy.Equals(other.Astroloy) && Cobalt.Equals(other.Cobalt) && Corn.Equals(other.Corn) && Wheat.Equals(other.Wheat) && Biomass.Equals(other.Biomass) && Soy.Equals(other.Soy) && Mushroom.Equals(other.Mushroom) && Sugar.Equals(other.Sugar) && Cocoa.Equals(other.Cocoa) && Cheese.Equals(other.Cheese) && Temperature.Equals(other.Temperature) && Pressure.Equals(other.Pressure))
		{
			return RequiredMix.Contains(other.RequiredMix);
		}
		return false;
	}

	public string ToString(RecipeReference reference)
	{
		_reagentSb.Clear();
		MakeString(Energy, _reagentSb);
		MakeString(Temperature, _reagentSb);
		MakeString(Pressure, _reagentSb);
		MakeString(RequiredMix, _reagentSb);
		int num = 0;
		MakeSmartString(reference, Reagent.AllReagents[num++], Flour, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Milk, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Egg, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Iron, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Gold, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Carbon, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Uranium, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Copper, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Steel, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Hydrocarbon, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Silver, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Nickel, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Lead, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Electrum, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Invar, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Constantan, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Solder, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Plastic, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Silicon, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], SalicylicAcid, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Alcohol, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Oil, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Potato, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Tomato, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Fenoxitone, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], ColorRed, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], ColorGreen, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], ColorBlue, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], ColorYellow, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], ColorOrange, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Pumpkin, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Rice, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Waspaloy, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Stellite, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Inconel, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Hastelloy, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Astroloy, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Cobalt, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Corn, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Wheat, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Biomass, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Soy, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Mushroom, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Sugar, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Cocoa, _reagentSb);
		MakeSmartString(reference, Reagent.AllReagents[num++], Cheese, _reagentSb);
		return _reagentSb.ToString();
	}

	public override string ToString()
	{
		_reagentSb.Clear();
		MakeString(Energy, _reagentSb);
		MakeString(Temperature, _reagentSb);
		MakeString(Pressure, _reagentSb);
		MakeString(RequiredMix, _reagentSb);
		MakeString("Flour", Flour, _reagentSb);
		MakeString("Milk", Milk, _reagentSb);
		MakeString("Egg", Egg, _reagentSb);
		MakeString("Iron", Iron, _reagentSb);
		MakeString("Gold", Gold, _reagentSb);
		MakeString("Carbon", Carbon, _reagentSb);
		MakeString("Uranium", Uranium, _reagentSb);
		MakeString("Copper", Copper, _reagentSb);
		MakeString("Steel", Steel, _reagentSb);
		MakeString("Hydrocarbon", Hydrocarbon, _reagentSb);
		MakeString("Silver", Silver, _reagentSb);
		MakeString("Nickel", Nickel, _reagentSb);
		MakeString("Lead", Lead, _reagentSb);
		MakeString("Electrum", Electrum, _reagentSb);
		MakeString("Invar", Invar, _reagentSb);
		MakeString("Constantan", Constantan, _reagentSb);
		MakeString("Solder", Solder, _reagentSb);
		MakeString("Plastic", Plastic, _reagentSb);
		MakeString("Silicon", Silicon, _reagentSb);
		MakeString("SalicylicAcid", SalicylicAcid, _reagentSb);
		MakeString("Alcohol", Alcohol, _reagentSb);
		MakeString("Oil", Oil, _reagentSb);
		MakeString("Potato", Potato, _reagentSb);
		MakeString("Tomato", Tomato, _reagentSb);
		MakeString("Fenoxitone", Fenoxitone, _reagentSb);
		MakeString("ColorRed", ColorRed, _reagentSb);
		MakeString("ColorGreen", ColorGreen, _reagentSb);
		MakeString("ColorBlue", ColorBlue, _reagentSb);
		MakeString("ColorYellow", ColorYellow, _reagentSb);
		MakeString("ColorOrange", ColorOrange, _reagentSb);
		MakeString("Pumpkin", Pumpkin, _reagentSb);
		MakeString("Rice", Rice, _reagentSb);
		MakeString("Waspaloy", Waspaloy, _reagentSb);
		MakeString("Stellite", Stellite, _reagentSb);
		MakeString("Inconel", Inconel, _reagentSb);
		MakeString("Hastelloy", Hastelloy, _reagentSb);
		MakeString("Astroloy", Astroloy, _reagentSb);
		MakeString("Cobalt", Cobalt, _reagentSb);
		MakeString("Corn", Corn, _reagentSb);
		MakeString("Wheat", Wheat, _reagentSb);
		MakeString("Biomass", Biomass, _reagentSb);
		MakeString("Soy", Soy, _reagentSb);
		MakeString("Mushroom", Mushroom, _reagentSb);
		MakeString("Sugar", Sugar, _reagentSb);
		MakeString("Cocoa", Cocoa, _reagentSb);
		MakeString("Cheese", Cheese, _reagentSb);
		return _reagentSb.ToString();
	}

	public string GetComparisonResult(ReagentMixture reagentMixture)
	{
		_reagentSb.Clear();
		MakeString(_reagentSb, Flour, reagentMixture.Flour);
		MakeString(_reagentSb, Milk, reagentMixture.Milk);
		MakeString(_reagentSb, Egg, reagentMixture.Egg);
		MakeString(_reagentSb, Iron, reagentMixture.Iron);
		MakeString(_reagentSb, Gold, reagentMixture.Gold);
		MakeString(_reagentSb, Carbon, reagentMixture.Carbon);
		MakeString(_reagentSb, Uranium, reagentMixture.Uranium);
		MakeString(_reagentSb, Copper, reagentMixture.Copper);
		MakeString(_reagentSb, Steel, reagentMixture.Steel);
		MakeString(_reagentSb, Hydrocarbon, reagentMixture.Hydrocarbon);
		MakeString(_reagentSb, Silver, reagentMixture.Silver);
		MakeString(_reagentSb, Nickel, reagentMixture.Nickel);
		MakeString(_reagentSb, Lead, reagentMixture.Lead);
		MakeString(_reagentSb, Electrum, reagentMixture.Electrum);
		MakeString(_reagentSb, Invar, reagentMixture.Invar);
		MakeString(_reagentSb, Constantan, reagentMixture.Constantan);
		MakeString(_reagentSb, Solder, reagentMixture.Solder);
		MakeString(_reagentSb, Plastic, reagentMixture.Plastic);
		MakeString(_reagentSb, Silicon, reagentMixture.Silicon);
		MakeString(_reagentSb, SalicylicAcid, reagentMixture.SalicylicAcid);
		MakeString(_reagentSb, Alcohol, reagentMixture.Alcohol);
		MakeString(_reagentSb, Oil, reagentMixture.Oil);
		MakeString(_reagentSb, Potato, reagentMixture.Potato);
		MakeString(_reagentSb, Tomato, reagentMixture.Tomato);
		MakeString(_reagentSb, Fenoxitone, reagentMixture.Fenoxitone);
		MakeString(_reagentSb, ColorRed, reagentMixture.ColorRed);
		MakeString(_reagentSb, ColorGreen, reagentMixture.ColorGreen);
		MakeString(_reagentSb, ColorBlue, reagentMixture.ColorBlue);
		MakeString(_reagentSb, ColorYellow, reagentMixture.ColorYellow);
		MakeString(_reagentSb, ColorOrange, reagentMixture.ColorOrange);
		MakeString(_reagentSb, Pumpkin, reagentMixture.Pumpkin);
		MakeString(_reagentSb, Rice, reagentMixture.Rice);
		MakeString(_reagentSb, Waspaloy, reagentMixture.Waspaloy);
		MakeString(_reagentSb, Stellite, reagentMixture.Stellite);
		MakeString(_reagentSb, Inconel, reagentMixture.Inconel);
		MakeString(_reagentSb, Hastelloy, reagentMixture.Hastelloy);
		MakeString(_reagentSb, Astroloy, reagentMixture.Astroloy);
		MakeString(_reagentSb, Cobalt, reagentMixture.Cobalt);
		MakeString(_reagentSb, Corn, reagentMixture.Corn);
		MakeString(_reagentSb, Wheat, reagentMixture.Wheat);
		MakeString(_reagentSb, Biomass, reagentMixture.Biomass);
		MakeString(_reagentSb, Soy, reagentMixture.Soy);
		MakeString(_reagentSb, Mushroom, reagentMixture.Mushroom);
		MakeString(_reagentSb, Sugar, reagentMixture.Sugar);
		MakeString(_reagentSb, Cocoa, reagentMixture.Cocoa);
		MakeString(_reagentSb, Cheese, reagentMixture.Cheese);
		return _reagentSb.ToString();
	}

	public override int GetHashCode()
	{
		return Flour.GetHashCode() ^ Milk.GetHashCode() ^ Egg.GetHashCode() ^ Iron.GetHashCode() ^ Gold.GetHashCode() ^ Carbon.GetHashCode() ^ Uranium.GetHashCode() ^ Copper.GetHashCode() ^ Steel.GetHashCode() ^ Hydrocarbon.GetHashCode() ^ Silver.GetHashCode() ^ Nickel.GetHashCode() ^ Lead.GetHashCode() ^ Electrum.GetHashCode() ^ Invar.GetHashCode() ^ Constantan.GetHashCode() ^ Solder.GetHashCode() ^ Plastic.GetHashCode() ^ Silicon.GetHashCode() ^ SalicylicAcid.GetHashCode() ^ Alcohol.GetHashCode() ^ Oil.GetHashCode() ^ Potato.GetHashCode() ^ Tomato.GetHashCode() ^ Fenoxitone.GetHashCode() ^ ColorRed.GetHashCode() ^ ColorGreen.GetHashCode() ^ ColorBlue.GetHashCode() ^ ColorYellow.GetHashCode() ^ ColorOrange.GetHashCode() ^ Pumpkin.GetHashCode() ^ Rice.GetHashCode() ^ Waspaloy.GetHashCode() ^ Stellite.GetHashCode() ^ Inconel.GetHashCode() ^ Hastelloy.GetHashCode() ^ Astroloy.GetHashCode() ^ Cobalt.GetHashCode() ^ Corn.GetHashCode() ^ Wheat.GetHashCode() ^ Biomass.GetHashCode() ^ Soy.GetHashCode() ^ Mushroom.GetHashCode() ^ Sugar.GetHashCode() ^ Cocoa.GetHashCode() ^ Cheese.GetHashCode();
	}

	public static Recipe operator *(Recipe left, double right)
	{
		left.Flour *= right;
		left.Milk *= right;
		left.Egg *= right;
		left.Iron *= right;
		left.Gold *= right;
		left.Carbon *= right;
		left.Uranium *= right;
		left.Copper *= right;
		left.Steel *= right;
		left.Hydrocarbon *= right;
		left.Silver *= right;
		left.Nickel *= right;
		left.Lead *= right;
		left.Electrum *= right;
		left.Invar *= right;
		left.Constantan *= right;
		left.Solder *= right;
		left.Plastic *= right;
		left.Silicon *= right;
		left.SalicylicAcid *= right;
		left.Alcohol *= right;
		left.Oil *= right;
		left.Potato *= right;
		left.Tomato *= right;
		left.Fenoxitone *= right;
		left.ColorRed *= right;
		left.ColorGreen *= right;
		left.ColorBlue *= right;
		left.ColorYellow *= right;
		left.ColorOrange *= right;
		left.Pumpkin *= right;
		left.Rice *= right;
		left.Waspaloy *= right;
		left.Stellite *= right;
		left.Inconel *= right;
		left.Hastelloy *= right;
		left.Astroloy *= right;
		left.Cobalt *= right;
		left.Corn *= right;
		left.Wheat *= right;
		left.Biomass *= right;
		left.Soy *= right;
		left.Mushroom *= right;
		left.Sugar *= right;
		left.Cocoa *= right;
		left.Cheese *= right;
		return left;
	}

	public double Get(Reagent reagent)
	{
		if (reagent is Flour)
		{
			return Flour;
		}
		if (reagent is Milk)
		{
			return Milk;
		}
		if (reagent is Egg)
		{
			return Egg;
		}
		if (reagent is Iron)
		{
			return Iron;
		}
		if (reagent is Gold)
		{
			return Gold;
		}
		if (reagent is Carbon)
		{
			return Carbon;
		}
		if (reagent is Uranium)
		{
			return Uranium;
		}
		if (reagent is Copper)
		{
			return Copper;
		}
		if (reagent is Steel)
		{
			return Steel;
		}
		if (reagent is Hydrocarbon)
		{
			return Hydrocarbon;
		}
		if (reagent is Silver)
		{
			return Silver;
		}
		if (reagent is Nickel)
		{
			return Nickel;
		}
		if (reagent is Lead)
		{
			return Lead;
		}
		if (reagent is Electrum)
		{
			return Electrum;
		}
		if (reagent is Invar)
		{
			return Invar;
		}
		if (reagent is Constantan)
		{
			return Constantan;
		}
		if (reagent is Solder)
		{
			return Solder;
		}
		if (reagent is Plastic)
		{
			return Plastic;
		}
		if (reagent is Silicon)
		{
			return Silicon;
		}
		if (reagent is SalicylicAcid)
		{
			return SalicylicAcid;
		}
		if (reagent is Alcohol)
		{
			return Alcohol;
		}
		if (reagent is Oil)
		{
			return Oil;
		}
		if (reagent is Potato)
		{
			return Potato;
		}
		if (reagent is Tomato)
		{
			return Tomato;
		}
		if (reagent is Fenoxitone)
		{
			return Fenoxitone;
		}
		if (reagent is ColorRed)
		{
			return ColorRed;
		}
		if (reagent is ColorGreen)
		{
			return ColorGreen;
		}
		if (reagent is ColorBlue)
		{
			return ColorBlue;
		}
		if (reagent is ColorYellow)
		{
			return ColorYellow;
		}
		if (reagent is ColorOrange)
		{
			return ColorOrange;
		}
		if (reagent is Pumpkin)
		{
			return Pumpkin;
		}
		if (reagent is Rice)
		{
			return Rice;
		}
		if (reagent is Waspaloy)
		{
			return Waspaloy;
		}
		if (reagent is Stellite)
		{
			return Stellite;
		}
		if (reagent is Inconel)
		{
			return Inconel;
		}
		if (reagent is Hastelloy)
		{
			return Hastelloy;
		}
		if (reagent is Astroloy)
		{
			return Astroloy;
		}
		if (reagent is Cobalt)
		{
			return Cobalt;
		}
		if (reagent is Corn)
		{
			return Corn;
		}
		if (reagent is Wheat)
		{
			return Wheat;
		}
		if (reagent is Biomass)
		{
			return Biomass;
		}
		if (reagent is Soy)
		{
			return Soy;
		}
		if (reagent is Mushroom)
		{
			return Mushroom;
		}
		if (reagent is Sugar)
		{
			return Sugar;
		}
		if (reagent is Cocoa)
		{
			return Cocoa;
		}
		if (reagent is Cheese)
		{
			return Cheese;
		}
		return 0.0;
	}

	public Recipe GetMissingReagents(ReagentMixture sourceMixture)
	{
		return new Recipe
		{
			Flour = ((sourceMixture.Flour.Quantity >= Flour) ? 0.0 : (Flour - sourceMixture.Flour.Quantity)),
			Milk = ((sourceMixture.Milk.Quantity >= Milk) ? 0.0 : (Milk - sourceMixture.Milk.Quantity)),
			Egg = ((sourceMixture.Egg.Quantity >= Egg) ? 0.0 : (Egg - sourceMixture.Egg.Quantity)),
			Iron = ((sourceMixture.Iron.Quantity >= Iron) ? 0.0 : (Iron - sourceMixture.Iron.Quantity)),
			Gold = ((sourceMixture.Gold.Quantity >= Gold) ? 0.0 : (Gold - sourceMixture.Gold.Quantity)),
			Carbon = ((sourceMixture.Carbon.Quantity >= Carbon) ? 0.0 : (Carbon - sourceMixture.Carbon.Quantity)),
			Uranium = ((sourceMixture.Uranium.Quantity >= Uranium) ? 0.0 : (Uranium - sourceMixture.Uranium.Quantity)),
			Copper = ((sourceMixture.Copper.Quantity >= Copper) ? 0.0 : (Copper - sourceMixture.Copper.Quantity)),
			Steel = ((sourceMixture.Steel.Quantity >= Steel) ? 0.0 : (Steel - sourceMixture.Steel.Quantity)),
			Hydrocarbon = ((sourceMixture.Hydrocarbon.Quantity >= Hydrocarbon) ? 0.0 : (Hydrocarbon - sourceMixture.Hydrocarbon.Quantity)),
			Silver = ((sourceMixture.Silver.Quantity >= Silver) ? 0.0 : (Silver - sourceMixture.Silver.Quantity)),
			Nickel = ((sourceMixture.Nickel.Quantity >= Nickel) ? 0.0 : (Nickel - sourceMixture.Nickel.Quantity)),
			Lead = ((sourceMixture.Lead.Quantity >= Lead) ? 0.0 : (Lead - sourceMixture.Lead.Quantity)),
			Electrum = ((sourceMixture.Electrum.Quantity >= Electrum) ? 0.0 : (Electrum - sourceMixture.Electrum.Quantity)),
			Invar = ((sourceMixture.Invar.Quantity >= Invar) ? 0.0 : (Invar - sourceMixture.Invar.Quantity)),
			Constantan = ((sourceMixture.Constantan.Quantity >= Constantan) ? 0.0 : (Constantan - sourceMixture.Constantan.Quantity)),
			Solder = ((sourceMixture.Solder.Quantity >= Solder) ? 0.0 : (Solder - sourceMixture.Solder.Quantity)),
			Plastic = ((sourceMixture.Plastic.Quantity >= Plastic) ? 0.0 : (Plastic - sourceMixture.Plastic.Quantity)),
			Silicon = ((sourceMixture.Silicon.Quantity >= Silicon) ? 0.0 : (Silicon - sourceMixture.Silicon.Quantity)),
			SalicylicAcid = ((sourceMixture.SalicylicAcid.Quantity >= SalicylicAcid) ? 0.0 : (SalicylicAcid - sourceMixture.SalicylicAcid.Quantity)),
			Alcohol = ((sourceMixture.Alcohol.Quantity >= Alcohol) ? 0.0 : (Alcohol - sourceMixture.Alcohol.Quantity)),
			Oil = ((sourceMixture.Oil.Quantity >= Oil) ? 0.0 : (Oil - sourceMixture.Oil.Quantity)),
			Potato = ((sourceMixture.Potato.Quantity >= Potato) ? 0.0 : (Potato - sourceMixture.Potato.Quantity)),
			Tomato = ((sourceMixture.Tomato.Quantity >= Tomato) ? 0.0 : (Tomato - sourceMixture.Tomato.Quantity)),
			Fenoxitone = ((sourceMixture.Fenoxitone.Quantity >= Fenoxitone) ? 0.0 : (Fenoxitone - sourceMixture.Fenoxitone.Quantity)),
			ColorRed = ((sourceMixture.ColorRed.Quantity >= ColorRed) ? 0.0 : (ColorRed - sourceMixture.ColorRed.Quantity)),
			ColorGreen = ((sourceMixture.ColorGreen.Quantity >= ColorGreen) ? 0.0 : (ColorGreen - sourceMixture.ColorGreen.Quantity)),
			ColorBlue = ((sourceMixture.ColorBlue.Quantity >= ColorBlue) ? 0.0 : (ColorBlue - sourceMixture.ColorBlue.Quantity)),
			ColorYellow = ((sourceMixture.ColorYellow.Quantity >= ColorYellow) ? 0.0 : (ColorYellow - sourceMixture.ColorYellow.Quantity)),
			ColorOrange = ((sourceMixture.ColorOrange.Quantity >= ColorOrange) ? 0.0 : (ColorOrange - sourceMixture.ColorOrange.Quantity)),
			Pumpkin = ((sourceMixture.Pumpkin.Quantity >= Pumpkin) ? 0.0 : (Pumpkin - sourceMixture.Pumpkin.Quantity)),
			Rice = ((sourceMixture.Rice.Quantity >= Rice) ? 0.0 : (Rice - sourceMixture.Rice.Quantity)),
			Waspaloy = ((sourceMixture.Waspaloy.Quantity >= Waspaloy) ? 0.0 : (Waspaloy - sourceMixture.Waspaloy.Quantity)),
			Stellite = ((sourceMixture.Stellite.Quantity >= Stellite) ? 0.0 : (Stellite - sourceMixture.Stellite.Quantity)),
			Inconel = ((sourceMixture.Inconel.Quantity >= Inconel) ? 0.0 : (Inconel - sourceMixture.Inconel.Quantity)),
			Hastelloy = ((sourceMixture.Hastelloy.Quantity >= Hastelloy) ? 0.0 : (Hastelloy - sourceMixture.Hastelloy.Quantity)),
			Astroloy = ((sourceMixture.Astroloy.Quantity >= Astroloy) ? 0.0 : (Astroloy - sourceMixture.Astroloy.Quantity)),
			Cobalt = ((sourceMixture.Cobalt.Quantity >= Cobalt) ? 0.0 : (Cobalt - sourceMixture.Cobalt.Quantity)),
			Corn = ((sourceMixture.Corn.Quantity >= Corn) ? 0.0 : (Corn - sourceMixture.Corn.Quantity)),
			Wheat = ((sourceMixture.Wheat.Quantity >= Wheat) ? 0.0 : (Wheat - sourceMixture.Wheat.Quantity)),
			Biomass = ((sourceMixture.Biomass.Quantity >= Biomass) ? 0.0 : (Biomass - sourceMixture.Biomass.Quantity)),
			Soy = ((sourceMixture.Soy.Quantity >= Soy) ? 0.0 : (Soy - sourceMixture.Soy.Quantity)),
			Mushroom = ((sourceMixture.Mushroom.Quantity >= Mushroom) ? 0.0 : (Mushroom - sourceMixture.Mushroom.Quantity)),
			Sugar = ((sourceMixture.Sugar.Quantity >= Sugar) ? 0.0 : (Sugar - sourceMixture.Sugar.Quantity)),
			Cocoa = ((sourceMixture.Cocoa.Quantity >= Cocoa) ? 0.0 : (Cocoa - sourceMixture.Cocoa.Quantity)),
			Cheese = ((sourceMixture.Cheese.Quantity >= Cheese) ? 0.0 : (Cheese - sourceMixture.Cocoa.Quantity))
		};
	}

	public override bool Equals(object obj)
	{
		if (!(obj is Recipe))
		{
			return false;
		}
		return Equals((Recipe)obj);
	}

	private void MakeSmartString(RecipeReference recipeReference, Reagent reagentType, double reagentQtyRequired, StringBuilder masterString, string color = "#B566FF")
	{
		if (reagentQtyRequired <= 0.0)
		{
			return;
		}
		masterString.Append("<color=yellow>");
		masterString.Append(StringManager.Get(reagentQtyRequired));
		masterString.Append("</color> x <link=Reagent");
		masterString.Append(reagentType.TypeNameShort);
		masterString.Append("><color=#B566FF>");
		masterString.Append(reagentType.DisplayName);
		masterString.Append("</color></link>");
		if (recipeReference.Creator is IResourceConsumer resourceConsumer && resourceConsumer.CanProcess(reagentType))
		{
			masterString.Append(GameStrings.RecipeReagentFrom);
			List<Item> resourcesUsed = resourceConsumer.GetResourcesUsed();
			int num = 0;
			foreach (Item item in resourcesUsed)
			{
				if (item.CreatedReagentMixture.Contains(reagentType))
				{
					if (num > 0)
					{
						masterString.Append(", ");
					}
					masterString.Append("<link=Thing");
					masterString.Append(item.PrefabName);
					masterString.Append("><color=green>");
					masterString.Append(item.DisplayName);
					masterString.Append("</color></link>");
					num++;
				}
			}
		}
		masterString.AppendLine();
	}

	private void MakeString(string name, double value, StringBuilder masterString, string color = "#B566FF")
	{
		if (!(value <= 0.0))
		{
			masterString.AppendLine("<color=yellow>" + StringManager.Get(value) + "</color> x <link=Reagent" + name + "><color=" + color + ">" + name + "</color></link>");
		}
	}

	private void MakeString(float value, StringBuilder masterString)
	{
		if (!(value <= 0f))
		{
			masterString.AppendLine(string.Format("<color=yellow>{0}</color> x <link=EnergyPage>{1}{2}</color></link>", StringManager.Get(value), "<color=#0080FFFF>", GameStrings.RecipeEnergy));
		}
	}

	private void MakeString(Temperature value, StringBuilder masterString)
	{
		if (value.IsValid)
		{
			masterString.AppendLine(string.Format("{0}: <color=yellow>{1} ({2}<sup>o</sup>C)</color>{3}<color=yellow>{4} ({5}<sup>o</sup>C)</color>", Localization.GetInterface("NewWorldTemperature"), value.Start.ToStringPrefix("K"), StringManager.Get(RocketMath.KelvinToCelsius(new TemperatureKelvin(value.Start))), GameStrings.RecipeTemperatureRangeSeporator, value.Stop.ToStringPrefix("K"), StringManager.Get(RocketMath.KelvinToCelsius(new TemperatureKelvin(value.Stop)))));
		}
	}

	private void MakeString(Pressure value, StringBuilder masterString)
	{
		if (value.IsValid)
		{
			masterString.AppendLine(string.Format("{0}: <color=yellow>{1}</color>{2}<color=yellow>{3}</color>", Localization.GetInterface("NewWorldPressure"), (value.Start * 1000f).ToStringPrefix("Pa"), GameStrings.RecipeTemperatureRangeSeporator, (value.Stop * 1000f).ToStringPrefix("Pa")));
		}
	}

	private void MakeString(MoleMixture value, StringBuilder masterString)
	{
		if (value.IsAny)
		{
			if (value.Rule == MixRule.Pure)
			{
				masterString.AppendLine(GameStrings.MoleMixtureMustBePure.DisplayString);
			}
			AddMixValue(masterString, Chemistry.GasType.Oxygen, value.Oxygen);
			AddMixValue(masterString, Chemistry.GasType.Nitrogen, value.Nitrogen);
			AddMixValue(masterString, Chemistry.GasType.CarbonDioxide, value.CarbonDioxide);
			AddMixValue(masterString, Chemistry.GasType.Methane, value.Methane);
			AddMixValue(masterString, Chemistry.GasType.Pollutant, value.Pollutant);
			AddMixValue(masterString, Chemistry.GasType.NitrousOxide, value.NitrousOxide);
			AddMixValue(masterString, Chemistry.GasType.Water, value.Water);
			AddMixValue(masterString, Chemistry.GasType.PollutedWater, value.PollutedWater);
			AddMixValue(masterString, Chemistry.GasType.LiquidNitrogen, value.LiquidNitrogen);
			AddMixValue(masterString, Chemistry.GasType.LiquidOxygen, value.LiquidOxygen);
			AddMixValue(masterString, Chemistry.GasType.LiquidMethane, value.LiquidMethane);
			AddMixValue(masterString, Chemistry.GasType.Steam, value.Steam);
			AddMixValue(masterString, Chemistry.GasType.LiquidCarbonDioxide, value.LiquidCarbonDioxide);
			AddMixValue(masterString, Chemistry.GasType.LiquidPollutant, value.LiquidPollutant);
			AddMixValue(masterString, Chemistry.GasType.LiquidNitrousOxide, value.LiquidNitrousOxide);
			AddMixValue(masterString, Chemistry.GasType.Hydrogen, value.Hydrogen);
			AddMixValue(masterString, Chemistry.GasType.LiquidHydrogen, value.LiquidHydrogen);
			AddMixValue(masterString, Chemistry.GasType.Hydrazine, value.Hydrazine);
			AddMixValue(masterString, Chemistry.GasType.LiquidHydrazine, value.LiquidHydrazine);
			AddMixValue(masterString, Chemistry.GasType.LiquidAlcohol, value.LiquidAlcohol);
			AddMixValue(masterString, Chemistry.GasType.Helium, value.Helium);
			AddMixValue(masterString, Chemistry.GasType.LiquidSodiumChloride, value.LiquidSodiumChloride);
			AddMixValue(masterString, Chemistry.GasType.Silanol, value.Silanol);
			AddMixValue(masterString, Chemistry.GasType.LiquidSilanol, value.LiquidSilanol);
			AddMixValue(masterString, Chemistry.GasType.HydrochloricAcid, value.HydrochloricAcid);
			AddMixValue(masterString, Chemistry.GasType.LiquidHydrochloricAcid, value.LiquidHydrochloricAcid);
			AddMixValue(masterString, Chemistry.GasType.Ozone, value.Ozone);
			AddMixValue(masterString, Chemistry.GasType.LiquidOzone, value.LiquidOzone);
		}
	}

	private void AddMixValue(StringBuilder tempString, Chemistry.GasType gasType, double value)
	{
		if (!double.IsNaN(value) && value != 0.0)
		{
			tempString.AppendLine(GameStrings.QuantityOfGasTypeInMix.AsString(value.ToStringRounded(), FormatGas(gasType)));
		}
	}

	private string FormatGas(Chemistry.GasType gasType)
	{
		return string.Format("<link=Gas{1}><color=#44AD83>{0}</color></link>", Localization.GetName(gasType), EnumCollections.GasTypes.GetName(gasType));
	}

	private void MakeString(StringBuilder masterString, double recipeQuantity, Reagent reagent)
	{
		if (!(recipeQuantity <= 0.0))
		{
			masterString.AppendLine(reagent.Quantity.ToStringPrefix(reagent.Unit, (recipeQuantity > reagent.Quantity) ? "red" : "yellow") + "/" + recipeQuantity.ToStringPrefix(reagent.Unit, "yellow") + " <color=#B566FF>" + reagent.DisplayName + "</color>");
		}
	}

	private void MakePackagableComparisonString(StringBuilder masterString, double recipeQuantity, Reagent reagent, string color = "#B566FF")
	{
		if (!(recipeQuantity <= 0.0))
		{
			string displayName = reagent.DisplayName;
			float value = (float)recipeQuantity;
			double quantity = reagent.Quantity;
			masterString.AppendLine(quantity.ToStringPrefix("", (recipeQuantity > reagent.Quantity) ? "red" : "yellow") + "/" + value.ToStringPrefix("", "yellow") + " <color=" + color + ">" + displayName + "</color>");
		}
	}

	public bool IsNaN()
	{
		if (!double.IsNaN(Flour) && !double.IsNaN(Milk) && !double.IsNaN(Egg) && !double.IsNaN(Iron) && !double.IsNaN(Gold) && !double.IsNaN(Carbon) && !double.IsNaN(Uranium) && !double.IsNaN(Copper) && !double.IsNaN(Steel) && !double.IsNaN(Hydrocarbon) && !double.IsNaN(Silver) && !double.IsNaN(Nickel) && !double.IsNaN(Lead) && !double.IsNaN(Electrum) && !double.IsNaN(Invar) && !double.IsNaN(Constantan) && !double.IsNaN(Solder) && !double.IsNaN(Plastic) && !double.IsNaN(Silicon) && !double.IsNaN(SalicylicAcid) && !double.IsNaN(Alcohol) && !double.IsNaN(Oil) && !double.IsNaN(Potato) && !double.IsNaN(Tomato) && !double.IsNaN(Fenoxitone) && !double.IsNaN(ColorRed) && !double.IsNaN(ColorGreen) && !double.IsNaN(ColorBlue) && !double.IsNaN(ColorYellow) && !double.IsNaN(ColorOrange) && !double.IsNaN(Pumpkin) && !double.IsNaN(Rice) && !double.IsNaN(Waspaloy) && !double.IsNaN(Stellite) && !double.IsNaN(Inconel) && !double.IsNaN(Hastelloy) && !double.IsNaN(Astroloy) && !double.IsNaN(Cobalt) && !double.IsNaN(Corn) && !double.IsNaN(Wheat) && !double.IsNaN(Biomass) && !double.IsNaN(Soy) && !double.IsNaN(Mushroom) && !double.IsNaN(Sugar) && !double.IsNaN(Cocoa))
		{
			return double.IsNaN(Cheese);
		}
		return true;
	}

	public void PopulateInto(byte opcode, LogicStack stack, int begin, int end, ushort scale)
	{
		int index = begin;
		Poke(opcode, stack, ref index, end, "Flour", Flour * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Milk", Milk * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Egg", Egg * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Iron", Iron * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Gold", Gold * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Carbon", Carbon * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Uranium", Uranium * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Copper", Copper * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Steel", Steel * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Hydrocarbon", Hydrocarbon * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Silver", Silver * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Nickel", Nickel * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Lead", Lead * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Electrum", Electrum * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Invar", Invar * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Constantan", Constantan * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Solder", Solder * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Plastic", Plastic * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Silicon", Silicon * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "SalicylicAcid", SalicylicAcid * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Alcohol", Alcohol * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Oil", Oil * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Potato", Potato * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Tomato", Tomato * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Fenoxitone", Fenoxitone * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "ColorRed", ColorRed * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "ColorGreen", ColorGreen * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "ColorBlue", ColorBlue * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "ColorYellow", ColorYellow * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "ColorOrange", ColorOrange * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Pumpkin", Pumpkin * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Rice", Rice * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Waspaloy", Waspaloy * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Stellite", Stellite * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Inconel", Inconel * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Hastelloy", Hastelloy * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Astroloy", Astroloy * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Cobalt", Cobalt * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Corn", Corn * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Wheat", Wheat * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Biomass", Biomass * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Soy", Soy * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Mushroom", Mushroom * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Sugar", Sugar * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Cocoa", Cocoa * (double)(int)scale);
		Poke(opcode, stack, ref index, end, "Cheese", Cheese * (double)(int)scale);
	}

	private void Poke(byte opcode, LogicStack stack, ref int index, int end, string reagentName, double quantity)
	{
		if (index <= end)
		{
			byte b = (byte)Math.Min(255.0, Math.Ceiling(quantity));
			if (b != 0)
			{
				int @int = Animator.StringToHash(reagentName);
				double value = ProgrammableChip.LongToDouble(LogicStack.PackByteInt32(opcode, b, @int));
				stack.Poke(ref index, value);
			}
		}
	}

	static Recipe()
	{
		INVALID = new Recipe(double.NaN);
		_reagentSb = new StringBuilder();
	}
}
