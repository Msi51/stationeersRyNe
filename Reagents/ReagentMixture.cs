using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Objects.Rockets.Mining;

namespace Reagents;

[Serializable]
public class ReagentMixture
{
	[NonSerialized]
	[XmlIgnore]
	public Thing Parent;

	public Flour Flour = new Flour(0.0);

	public Milk Milk = new Milk(0.0);

	public Egg Egg = new Egg(0.0);

	public Iron Iron = new Iron(0.0);

	public Gold Gold = new Gold(0.0);

	public Carbon Carbon = new Carbon(0.0);

	public Uranium Uranium = new Uranium(0.0);

	public Copper Copper = new Copper(0.0);

	public Steel Steel = new Steel(0.0);

	public Hydrocarbon Hydrocarbon = new Hydrocarbon(0.0);

	public Silver Silver = new Silver(0.0);

	public Nickel Nickel = new Nickel(0.0);

	public Lead Lead = new Lead(0.0);

	public Electrum Electrum = new Electrum(0.0);

	public Invar Invar = new Invar(0.0);

	public Constantan Constantan = new Constantan(0.0);

	public Solder Solder = new Solder(0.0);

	public Plastic Plastic = new Plastic(0.0);

	public Silicon Silicon = new Silicon(0.0);

	public SalicylicAcid SalicylicAcid = new SalicylicAcid(0.0);

	public Alcohol Alcohol = new Alcohol(0.0);

	public Oil Oil = new Oil(0.0);

	public Potato Potato = new Potato(0.0);

	public Tomato Tomato = new Tomato(0.0);

	public Fenoxitone Fenoxitone = new Fenoxitone(0.0);

	public ColorRed ColorRed = new ColorRed(0.0);

	public ColorGreen ColorGreen = new ColorGreen(0.0);

	public ColorBlue ColorBlue = new ColorBlue(0.0);

	public ColorYellow ColorYellow = new ColorYellow(0.0);

	public ColorOrange ColorOrange = new ColorOrange(0.0);

	public Pumpkin Pumpkin = new Pumpkin(0.0);

	public Rice Rice = new Rice(0.0);

	public Waspaloy Waspaloy = new Waspaloy(0.0);

	public Stellite Stellite = new Stellite(0.0);

	public Inconel Inconel = new Inconel(0.0);

	public Hastelloy Hastelloy = new Hastelloy(0.0);

	public Astroloy Astroloy = new Astroloy(0.0);

	public Cobalt Cobalt = new Cobalt(0.0);

	public Corn Corn = new Corn(0.0);

	public Wheat Wheat = new Wheat(0.0);

	public Biomass Biomass = new Biomass(0.0);

	public Soy Soy = new Soy(0.0);

	public Mushroom Mushroom = new Mushroom(0.0);

	public Sugar Sugar = new Sugar(0.0);

	public Cocoa Cocoa = new Cocoa(0.0);

	public Cheese Cheese = new Cheese(0.0);

	public static readonly ReagentMixture Empty = new ReagentMixture();

	private string _reagentString;

	private List<ReagentSaveData> _saveData;

	public HeatCapacity HeatCapacity => new HeatCapacity(Flour.HeatCapacity + Milk.HeatCapacity + Egg.HeatCapacity + Iron.HeatCapacity + Gold.HeatCapacity + Carbon.HeatCapacity + Uranium.HeatCapacity + Copper.HeatCapacity + Steel.HeatCapacity + Hydrocarbon.HeatCapacity + Silver.HeatCapacity + Nickel.HeatCapacity + Lead.HeatCapacity + Electrum.HeatCapacity + Invar.HeatCapacity + Constantan.HeatCapacity + Solder.HeatCapacity + Plastic.HeatCapacity + Silicon.HeatCapacity + SalicylicAcid.HeatCapacity + Alcohol.HeatCapacity + Oil.HeatCapacity + Potato.HeatCapacity + Tomato.HeatCapacity + Fenoxitone.HeatCapacity + ColorRed.HeatCapacity + ColorGreen.HeatCapacity + ColorBlue.HeatCapacity + ColorYellow.HeatCapacity + ColorOrange.HeatCapacity + Pumpkin.HeatCapacity + Rice.HeatCapacity + Waspaloy.HeatCapacity + Stellite.HeatCapacity + Inconel.HeatCapacity + Hastelloy.HeatCapacity + Astroloy.HeatCapacity + Cobalt.HeatCapacity + Corn.HeatCapacity + Wheat.HeatCapacity + Biomass.HeatCapacity + Soy.HeatCapacity + Mushroom.HeatCapacity + Sugar.HeatCapacity + Cocoa.HeatCapacity + Cheese.HeatCapacity);

	public double TotalReagents => Flour.Quantity + Milk.Quantity + Egg.Quantity + Iron.Quantity + Gold.Quantity + Carbon.Quantity + Uranium.Quantity + Copper.Quantity + Steel.Quantity + Hydrocarbon.Quantity + Silver.Quantity + Nickel.Quantity + Lead.Quantity + Electrum.Quantity + Invar.Quantity + Constantan.Quantity + Solder.Quantity + Plastic.Quantity + Silicon.Quantity + SalicylicAcid.Quantity + Alcohol.Quantity + Oil.Quantity + Potato.Quantity + Tomato.Quantity + Fenoxitone.Quantity + ColorRed.Quantity + ColorGreen.Quantity + ColorBlue.Quantity + ColorYellow.Quantity + ColorOrange.Quantity + Pumpkin.Quantity + Rice.Quantity + Waspaloy.Quantity + Stellite.Quantity + Inconel.Quantity + Hastelloy.Quantity + Astroloy.Quantity + Cobalt.Quantity + Corn.Quantity + Wheat.Quantity + Biomass.Quantity + Soy.Quantity + Mushroom.Quantity + Sugar.Quantity + Cocoa.Quantity + Cheese.Quantity;

	public static ReagentMixture IngredientListToMixture(List<ReagentMixIngredientSaveData> dataList)
	{
		ReagentMixture reagentMixture = null;
		foreach (ReagentMixIngredientSaveData data in dataList)
		{
			Reagent reagent = Reagent.Generate(data.ReagentName);
			if (reagent != null)
			{
				reagent.Quantity = data.Quantity;
				if (reagentMixture == null)
				{
					reagentMixture = new ReagentMixture(reagent);
				}
				else
				{
					reagentMixture.Add(reagent);
				}
				if (reagentMixture.TotalReagents > 1.0)
				{
					reagentMixture = reagentMixture.GetRatioMixture();
				}
			}
		}
		return reagentMixture;
	}

	public List<ReagentMixIngredientSaveData> ToIngredientList()
	{
		List<ReagentMixIngredientSaveData> list = new List<ReagentMixIngredientSaveData>();
		Serialize(list, Flour);
		Serialize(list, Milk);
		Serialize(list, Egg);
		Serialize(list, Iron);
		Serialize(list, Gold);
		Serialize(list, Carbon);
		Serialize(list, Uranium);
		Serialize(list, Copper);
		Serialize(list, Steel);
		Serialize(list, Hydrocarbon);
		Serialize(list, Silver);
		Serialize(list, Nickel);
		Serialize(list, Lead);
		Serialize(list, Electrum);
		Serialize(list, Invar);
		Serialize(list, Constantan);
		Serialize(list, Solder);
		Serialize(list, Plastic);
		Serialize(list, Silicon);
		Serialize(list, SalicylicAcid);
		Serialize(list, Alcohol);
		Serialize(list, Oil);
		Serialize(list, Potato);
		Serialize(list, Tomato);
		Serialize(list, Fenoxitone);
		Serialize(list, ColorRed);
		Serialize(list, ColorGreen);
		Serialize(list, ColorBlue);
		Serialize(list, ColorYellow);
		Serialize(list, ColorOrange);
		Serialize(list, Pumpkin);
		Serialize(list, Rice);
		Serialize(list, Waspaloy);
		Serialize(list, Stellite);
		Serialize(list, Inconel);
		Serialize(list, Hastelloy);
		Serialize(list, Astroloy);
		Serialize(list, Cobalt);
		Serialize(list, Corn);
		Serialize(list, Wheat);
		Serialize(list, Biomass);
		Serialize(list, Soy);
		Serialize(list, Mushroom);
		Serialize(list, Sugar);
		Serialize(list, Cocoa);
		Serialize(list, Cheese);
		return list;
	}

	public List<ReagentSaveData> Serialize()
	{
		_saveData = new List<ReagentSaveData>();
		Serialize(Flour);
		Serialize(Milk);
		Serialize(Egg);
		Serialize(Iron);
		Serialize(Gold);
		Serialize(Carbon);
		Serialize(Uranium);
		Serialize(Copper);
		Serialize(Steel);
		Serialize(Hydrocarbon);
		Serialize(Silver);
		Serialize(Nickel);
		Serialize(Lead);
		Serialize(Electrum);
		Serialize(Invar);
		Serialize(Constantan);
		Serialize(Solder);
		Serialize(Plastic);
		Serialize(Silicon);
		Serialize(SalicylicAcid);
		Serialize(Alcohol);
		Serialize(Oil);
		Serialize(Potato);
		Serialize(Tomato);
		Serialize(Fenoxitone);
		Serialize(ColorRed);
		Serialize(ColorGreen);
		Serialize(ColorBlue);
		Serialize(ColorYellow);
		Serialize(ColorOrange);
		Serialize(Pumpkin);
		Serialize(Rice);
		Serialize(Waspaloy);
		Serialize(Stellite);
		Serialize(Inconel);
		Serialize(Hastelloy);
		Serialize(Astroloy);
		Serialize(Cobalt);
		Serialize(Corn);
		Serialize(Wheat);
		Serialize(Biomass);
		Serialize(Soy);
		Serialize(Mushroom);
		Serialize(Sugar);
		Serialize(Cocoa);
		Serialize(Cheese);
		return _saveData;
	}

	public void Initialize()
	{
		Flour = new Flour(0.0);
		Flour.ParentMixture = this;
		Milk = new Milk(0.0);
		Milk.ParentMixture = this;
		Egg = new Egg(0.0);
		Egg.ParentMixture = this;
		Iron = new Iron(0.0);
		Iron.ParentMixture = this;
		Gold = new Gold(0.0);
		Gold.ParentMixture = this;
		Carbon = new Carbon(0.0);
		Carbon.ParentMixture = this;
		Uranium = new Uranium(0.0);
		Uranium.ParentMixture = this;
		Copper = new Copper(0.0);
		Copper.ParentMixture = this;
		Steel = new Steel(0.0);
		Steel.ParentMixture = this;
		Hydrocarbon = new Hydrocarbon(0.0);
		Hydrocarbon.ParentMixture = this;
		Silver = new Silver(0.0);
		Silver.ParentMixture = this;
		Nickel = new Nickel(0.0);
		Nickel.ParentMixture = this;
		Lead = new Lead(0.0);
		Lead.ParentMixture = this;
		Electrum = new Electrum(0.0);
		Electrum.ParentMixture = this;
		Invar = new Invar(0.0);
		Invar.ParentMixture = this;
		Constantan = new Constantan(0.0);
		Constantan.ParentMixture = this;
		Solder = new Solder(0.0);
		Solder.ParentMixture = this;
		Plastic = new Plastic(0.0);
		Plastic.ParentMixture = this;
		Silicon = new Silicon(0.0);
		Silicon.ParentMixture = this;
		SalicylicAcid = new SalicylicAcid(0.0);
		SalicylicAcid.ParentMixture = this;
		Alcohol = new Alcohol(0.0);
		Alcohol.ParentMixture = this;
		Oil = new Oil(0.0);
		Oil.ParentMixture = this;
		Potato = new Potato(0.0);
		Potato.ParentMixture = this;
		Tomato = new Tomato(0.0);
		Tomato.ParentMixture = this;
		Fenoxitone = new Fenoxitone(0.0);
		Fenoxitone.ParentMixture = this;
		ColorRed = new ColorRed(0.0);
		ColorRed.ParentMixture = this;
		ColorGreen = new ColorGreen(0.0);
		ColorGreen.ParentMixture = this;
		ColorBlue = new ColorBlue(0.0);
		ColorBlue.ParentMixture = this;
		ColorYellow = new ColorYellow(0.0);
		ColorYellow.ParentMixture = this;
		ColorOrange = new ColorOrange(0.0);
		ColorOrange.ParentMixture = this;
		Pumpkin = new Pumpkin(0.0);
		Pumpkin.ParentMixture = this;
		Rice = new Rice(0.0);
		Rice.ParentMixture = this;
		Waspaloy = new Waspaloy(0.0);
		Waspaloy.ParentMixture = this;
		Stellite = new Stellite(0.0);
		Stellite.ParentMixture = this;
		Inconel = new Inconel(0.0);
		Inconel.ParentMixture = this;
		Hastelloy = new Hastelloy(0.0);
		Hastelloy.ParentMixture = this;
		Astroloy = new Astroloy(0.0);
		Astroloy.ParentMixture = this;
		Cobalt = new Cobalt(0.0);
		Cobalt.ParentMixture = this;
		Corn = new Corn(0.0);
		Corn.ParentMixture = this;
		Wheat = new Wheat(0.0);
		Wheat.ParentMixture = this;
		Biomass = new Biomass(0.0);
		Biomass.ParentMixture = this;
		Soy = new Soy(0.0);
		Soy.ParentMixture = this;
		Mushroom = new Mushroom(0.0);
		Mushroom.ParentMixture = this;
		Sugar = new Sugar(0.0);
		Sugar.ParentMixture = this;
		Cocoa = new Cocoa(0.0);
		Cocoa.ParentMixture = this;
		Cheese = new Cheese(0.0);
		Cheese.ParentMixture = this;
	}

	public ReagentMixture(double quantity)
	{
		Flour = new Flour(quantity);
		Milk = new Milk(quantity);
		Egg = new Egg(quantity);
		Iron = new Iron(quantity);
		Gold = new Gold(quantity);
		Carbon = new Carbon(quantity);
		Uranium = new Uranium(quantity);
		Copper = new Copper(quantity);
		Steel = new Steel(quantity);
		Hydrocarbon = new Hydrocarbon(quantity);
		Silver = new Silver(quantity);
		Nickel = new Nickel(quantity);
		Lead = new Lead(quantity);
		Electrum = new Electrum(quantity);
		Invar = new Invar(quantity);
		Constantan = new Constantan(quantity);
		Solder = new Solder(quantity);
		Plastic = new Plastic(quantity);
		Silicon = new Silicon(quantity);
		SalicylicAcid = new SalicylicAcid(quantity);
		Alcohol = new Alcohol(quantity);
		Oil = new Oil(quantity);
		Potato = new Potato(quantity);
		Tomato = new Tomato(quantity);
		Fenoxitone = new Fenoxitone(quantity);
		ColorRed = new ColorRed(quantity);
		ColorGreen = new ColorGreen(quantity);
		ColorBlue = new ColorBlue(quantity);
		ColorYellow = new ColorYellow(quantity);
		ColorOrange = new ColorOrange(quantity);
		Pumpkin = new Pumpkin(quantity);
		Rice = new Rice(quantity);
		Waspaloy = new Waspaloy(quantity);
		Stellite = new Stellite(quantity);
		Inconel = new Inconel(quantity);
		Hastelloy = new Hastelloy(quantity);
		Astroloy = new Astroloy(quantity);
		Cobalt = new Cobalt(quantity);
		Corn = new Corn(quantity);
		Wheat = new Wheat(quantity);
		Biomass = new Biomass(quantity);
		Soy = new Soy(quantity);
		Mushroom = new Mushroom(quantity);
		Sugar = new Sugar(quantity);
		Cocoa = new Cocoa(quantity);
		Cheese = new Cheese(quantity);
	}

	public ReagentMixture(Recipe recipe)
	{
		Flour = new Flour(recipe.Flour);
		Milk = new Milk(recipe.Milk);
		Egg = new Egg(recipe.Egg);
		Iron = new Iron(recipe.Iron);
		Gold = new Gold(recipe.Gold);
		Carbon = new Carbon(recipe.Carbon);
		Uranium = new Uranium(recipe.Uranium);
		Copper = new Copper(recipe.Copper);
		Steel = new Steel(recipe.Steel);
		Hydrocarbon = new Hydrocarbon(recipe.Hydrocarbon);
		Silver = new Silver(recipe.Silver);
		Nickel = new Nickel(recipe.Nickel);
		Lead = new Lead(recipe.Lead);
		Electrum = new Electrum(recipe.Electrum);
		Invar = new Invar(recipe.Invar);
		Constantan = new Constantan(recipe.Constantan);
		Solder = new Solder(recipe.Solder);
		Plastic = new Plastic(recipe.Plastic);
		Silicon = new Silicon(recipe.Silicon);
		SalicylicAcid = new SalicylicAcid(recipe.SalicylicAcid);
		Alcohol = new Alcohol(recipe.Alcohol);
		Oil = new Oil(recipe.Oil);
		Potato = new Potato(recipe.Potato);
		Tomato = new Tomato(recipe.Tomato);
		Fenoxitone = new Fenoxitone(recipe.Fenoxitone);
		ColorRed = new ColorRed(recipe.ColorRed);
		ColorGreen = new ColorGreen(recipe.ColorGreen);
		ColorBlue = new ColorBlue(recipe.ColorBlue);
		ColorYellow = new ColorYellow(recipe.ColorYellow);
		ColorOrange = new ColorOrange(recipe.ColorOrange);
		Pumpkin = new Pumpkin(recipe.Pumpkin);
		Rice = new Rice(recipe.Rice);
		Waspaloy = new Waspaloy(recipe.Waspaloy);
		Stellite = new Stellite(recipe.Stellite);
		Inconel = new Inconel(recipe.Inconel);
		Hastelloy = new Hastelloy(recipe.Hastelloy);
		Astroloy = new Astroloy(recipe.Astroloy);
		Cobalt = new Cobalt(recipe.Cobalt);
		Corn = new Corn(recipe.Corn);
		Wheat = new Wheat(recipe.Wheat);
		Biomass = new Biomass(recipe.Biomass);
		Soy = new Soy(recipe.Soy);
		Mushroom = new Mushroom(recipe.Mushroom);
		Sugar = new Sugar(recipe.Sugar);
		Cocoa = new Cocoa(recipe.Cocoa);
		Cheese = new Cheese(recipe.Cheese);
	}

	public ReagentMixture(ReagentMixture reagentMixture)
	{
		Flour = new Flour(reagentMixture.Flour.Quantity);
		Flour.ParentMixture = this;
		Milk = new Milk(reagentMixture.Milk.Quantity);
		Milk.ParentMixture = this;
		Egg = new Egg(reagentMixture.Egg.Quantity);
		Egg.ParentMixture = this;
		Iron = new Iron(reagentMixture.Iron.Quantity);
		Iron.ParentMixture = this;
		Gold = new Gold(reagentMixture.Gold.Quantity);
		Gold.ParentMixture = this;
		Carbon = new Carbon(reagentMixture.Carbon.Quantity);
		Carbon.ParentMixture = this;
		Uranium = new Uranium(reagentMixture.Uranium.Quantity);
		Uranium.ParentMixture = this;
		Copper = new Copper(reagentMixture.Copper.Quantity);
		Copper.ParentMixture = this;
		Steel = new Steel(reagentMixture.Steel.Quantity);
		Steel.ParentMixture = this;
		Hydrocarbon = new Hydrocarbon(reagentMixture.Hydrocarbon.Quantity);
		Hydrocarbon.ParentMixture = this;
		Silver = new Silver(reagentMixture.Silver.Quantity);
		Silver.ParentMixture = this;
		Nickel = new Nickel(reagentMixture.Nickel.Quantity);
		Nickel.ParentMixture = this;
		Lead = new Lead(reagentMixture.Lead.Quantity);
		Lead.ParentMixture = this;
		Electrum = new Electrum(reagentMixture.Electrum.Quantity);
		Electrum.ParentMixture = this;
		Invar = new Invar(reagentMixture.Invar.Quantity);
		Invar.ParentMixture = this;
		Constantan = new Constantan(reagentMixture.Constantan.Quantity);
		Constantan.ParentMixture = this;
		Solder = new Solder(reagentMixture.Solder.Quantity);
		Solder.ParentMixture = this;
		Plastic = new Plastic(reagentMixture.Plastic.Quantity);
		Plastic.ParentMixture = this;
		Silicon = new Silicon(reagentMixture.Silicon.Quantity);
		Silicon.ParentMixture = this;
		SalicylicAcid = new SalicylicAcid(reagentMixture.SalicylicAcid.Quantity);
		SalicylicAcid.ParentMixture = this;
		Alcohol = new Alcohol(reagentMixture.Alcohol.Quantity);
		Alcohol.ParentMixture = this;
		Oil = new Oil(reagentMixture.Oil.Quantity);
		Oil.ParentMixture = this;
		Potato = new Potato(reagentMixture.Potato.Quantity);
		Potato.ParentMixture = this;
		Tomato = new Tomato(reagentMixture.Tomato.Quantity);
		Tomato.ParentMixture = this;
		Fenoxitone = new Fenoxitone(reagentMixture.Fenoxitone.Quantity);
		Fenoxitone.ParentMixture = this;
		ColorRed = new ColorRed(reagentMixture.ColorRed.Quantity);
		ColorRed.ParentMixture = this;
		ColorGreen = new ColorGreen(reagentMixture.ColorGreen.Quantity);
		ColorGreen.ParentMixture = this;
		ColorBlue = new ColorBlue(reagentMixture.ColorBlue.Quantity);
		ColorBlue.ParentMixture = this;
		ColorYellow = new ColorYellow(reagentMixture.ColorYellow.Quantity);
		ColorYellow.ParentMixture = this;
		ColorOrange = new ColorOrange(reagentMixture.ColorOrange.Quantity);
		ColorOrange.ParentMixture = this;
		Pumpkin = new Pumpkin(reagentMixture.Pumpkin.Quantity);
		Pumpkin.ParentMixture = this;
		Rice = new Rice(reagentMixture.Rice.Quantity);
		Rice.ParentMixture = this;
		Waspaloy = new Waspaloy(reagentMixture.Waspaloy.Quantity);
		Waspaloy.ParentMixture = this;
		Stellite = new Stellite(reagentMixture.Stellite.Quantity);
		Stellite.ParentMixture = this;
		Inconel = new Inconel(reagentMixture.Inconel.Quantity);
		Inconel.ParentMixture = this;
		Hastelloy = new Hastelloy(reagentMixture.Hastelloy.Quantity);
		Hastelloy.ParentMixture = this;
		Astroloy = new Astroloy(reagentMixture.Astroloy.Quantity);
		Astroloy.ParentMixture = this;
		Cobalt = new Cobalt(reagentMixture.Cobalt.Quantity);
		Cobalt.ParentMixture = this;
		Corn = new Corn(reagentMixture.Corn.Quantity);
		Corn.ParentMixture = this;
		Wheat = new Wheat(reagentMixture.Wheat.Quantity);
		Wheat.ParentMixture = this;
		Biomass = new Biomass(reagentMixture.Biomass.Quantity);
		Biomass.ParentMixture = this;
		Soy = new Soy(reagentMixture.Soy.Quantity);
		Soy.ParentMixture = this;
		Mushroom = new Mushroom(reagentMixture.Mushroom.Quantity);
		Mushroom.ParentMixture = this;
		Sugar = new Sugar(reagentMixture.Sugar.Quantity);
		Sugar.ParentMixture = this;
		Cocoa = new Cocoa(reagentMixture.Cocoa.Quantity);
		Cocoa.ParentMixture = this;
		Cheese = new Cheese(reagentMixture.Cheese.Quantity);
		Cheese.ParentMixture = this;
	}

	public int AddNextReagent(ReagentMixture reagentMix, int max)
	{
		if (TotalReagents <= 0.0)
		{
			return 0;
		}
		double quantity;
		Reagent nextReagent = GetNextReagent(max, out quantity);
		Subtract(nextReagent);
		ReagentMixture reagentMixture = new ReagentMixture();
		reagentMixture.Add(nextReagent);
		reagentMix.Add(reagentMixture.GetRatioMixture());
		return (int)quantity;
	}

	public ReagentMixture GetNextMix()
	{
		double quantity;
		Reagent nextReagent = GetNextReagent(9999, out quantity);
		ReagentMixture reagentMixture = new ReagentMixture();
		reagentMixture.Add(nextReagent);
		return reagentMixture.GetRatioMixture();
	}

	public T Take<T>(T reagentRequest, double quantity) where T : Reagent, new()
	{
		quantity = Math.Min(quantity, Get(reagentRequest));
		if (quantity <= 0.0)
		{
			return null;
		}
		T val = (T)Activator.CreateInstance(reagentRequest.GetType());
		val.Quantity = quantity;
		Subtract(val);
		return val;
	}

	private Reagent GetNextReagent(int max, out double quantity)
	{
		Reagent result = new Reagent();
		quantity = 0.0;
		if (Flour.Quantity >= 1.0)
		{
			quantity = GetQuantity(Flour, max);
			result = new Flour(quantity);
		}
		if (Milk.Quantity >= 1.0)
		{
			quantity = GetQuantity(Milk, max);
			result = new Milk(quantity);
		}
		if (Egg.Quantity >= 1.0)
		{
			quantity = GetQuantity(Egg, max);
			result = new Egg(quantity);
		}
		if (Iron.Quantity >= 1.0)
		{
			quantity = GetQuantity(Iron, max);
			result = new Iron(quantity);
		}
		if (Gold.Quantity >= 1.0)
		{
			quantity = GetQuantity(Gold, max);
			result = new Gold(quantity);
		}
		if (Carbon.Quantity >= 1.0)
		{
			quantity = GetQuantity(Carbon, max);
			result = new Carbon(quantity);
		}
		if (Uranium.Quantity >= 1.0)
		{
			quantity = GetQuantity(Uranium, max);
			result = new Uranium(quantity);
		}
		if (Copper.Quantity >= 1.0)
		{
			quantity = GetQuantity(Copper, max);
			result = new Copper(quantity);
		}
		if (Steel.Quantity >= 1.0)
		{
			quantity = GetQuantity(Steel, max);
			result = new Steel(quantity);
		}
		if (Hydrocarbon.Quantity >= 1.0)
		{
			quantity = GetQuantity(Hydrocarbon, max);
			result = new Hydrocarbon(quantity);
		}
		if (Silver.Quantity >= 1.0)
		{
			quantity = GetQuantity(Silver, max);
			result = new Silver(quantity);
		}
		if (Nickel.Quantity >= 1.0)
		{
			quantity = GetQuantity(Nickel, max);
			result = new Nickel(quantity);
		}
		if (Lead.Quantity >= 1.0)
		{
			quantity = GetQuantity(Lead, max);
			result = new Lead(quantity);
		}
		if (Electrum.Quantity >= 1.0)
		{
			quantity = GetQuantity(Electrum, max);
			result = new Electrum(quantity);
		}
		if (Invar.Quantity >= 1.0)
		{
			quantity = GetQuantity(Invar, max);
			result = new Invar(quantity);
		}
		if (Constantan.Quantity >= 1.0)
		{
			quantity = GetQuantity(Constantan, max);
			result = new Constantan(quantity);
		}
		if (Solder.Quantity >= 1.0)
		{
			quantity = GetQuantity(Solder, max);
			result = new Solder(quantity);
		}
		if (Plastic.Quantity >= 1.0)
		{
			quantity = GetQuantity(Plastic, max);
			result = new Plastic(quantity);
		}
		if (Silicon.Quantity >= 1.0)
		{
			quantity = GetQuantity(Silicon, max);
			result = new Silicon(quantity);
		}
		if (SalicylicAcid.Quantity >= 1.0)
		{
			quantity = GetQuantity(SalicylicAcid, max);
			result = new SalicylicAcid(quantity);
		}
		if (Alcohol.Quantity >= 1.0)
		{
			quantity = GetQuantity(Alcohol, max);
			result = new Alcohol(quantity);
		}
		if (Oil.Quantity >= 1.0)
		{
			quantity = GetQuantity(Oil, max);
			result = new Oil(quantity);
		}
		if (Potato.Quantity >= 1.0)
		{
			quantity = GetQuantity(Potato, max);
			result = new Potato(quantity);
		}
		if (Tomato.Quantity >= 1.0)
		{
			quantity = GetQuantity(Tomato, max);
			result = new Tomato(quantity);
		}
		if (Fenoxitone.Quantity >= 1.0)
		{
			quantity = GetQuantity(Fenoxitone, max);
			result = new Fenoxitone(quantity);
		}
		if (ColorRed.Quantity >= 1.0)
		{
			quantity = GetQuantity(ColorRed, max);
			result = new ColorRed(quantity);
		}
		if (ColorGreen.Quantity >= 1.0)
		{
			quantity = GetQuantity(ColorGreen, max);
			result = new ColorGreen(quantity);
		}
		if (ColorBlue.Quantity >= 1.0)
		{
			quantity = GetQuantity(ColorBlue, max);
			result = new ColorBlue(quantity);
		}
		if (ColorYellow.Quantity >= 1.0)
		{
			quantity = GetQuantity(ColorYellow, max);
			result = new ColorYellow(quantity);
		}
		if (ColorOrange.Quantity >= 1.0)
		{
			quantity = GetQuantity(ColorOrange, max);
			result = new ColorOrange(quantity);
		}
		if (Pumpkin.Quantity >= 1.0)
		{
			quantity = GetQuantity(Pumpkin, max);
			result = new Pumpkin(quantity);
		}
		if (Rice.Quantity >= 1.0)
		{
			quantity = GetQuantity(Rice, max);
			result = new Rice(quantity);
		}
		if (Waspaloy.Quantity >= 1.0)
		{
			quantity = GetQuantity(Waspaloy, max);
			result = new Waspaloy(quantity);
		}
		if (Stellite.Quantity >= 1.0)
		{
			quantity = GetQuantity(Stellite, max);
			result = new Stellite(quantity);
		}
		if (Inconel.Quantity >= 1.0)
		{
			quantity = GetQuantity(Inconel, max);
			result = new Inconel(quantity);
		}
		if (Hastelloy.Quantity >= 1.0)
		{
			quantity = GetQuantity(Hastelloy, max);
			result = new Hastelloy(quantity);
		}
		if (Astroloy.Quantity >= 1.0)
		{
			quantity = GetQuantity(Astroloy, max);
			result = new Astroloy(quantity);
		}
		if (Cobalt.Quantity >= 1.0)
		{
			quantity = GetQuantity(Cobalt, max);
			result = new Cobalt(quantity);
		}
		if (Corn.Quantity >= 1.0)
		{
			quantity = GetQuantity(Corn, max);
			result = new Corn(quantity);
		}
		if (Wheat.Quantity >= 1.0)
		{
			quantity = GetQuantity(Wheat, max);
			result = new Wheat(quantity);
		}
		if (Biomass.Quantity >= 1.0)
		{
			quantity = GetQuantity(Biomass, max);
			result = new Biomass(quantity);
		}
		if (Soy.Quantity >= 1.0)
		{
			quantity = GetQuantity(Soy, max);
			result = new Soy(quantity);
		}
		if (Mushroom.Quantity >= 1.0)
		{
			quantity = GetQuantity(Mushroom, max);
			result = new Mushroom(quantity);
		}
		if (Sugar.Quantity >= 1.0)
		{
			quantity = GetQuantity(Sugar, max);
			result = new Sugar(quantity);
		}
		if (Cocoa.Quantity >= 1.0)
		{
			quantity = GetQuantity(Cocoa, max);
			result = new Cocoa(quantity);
		}
		if (Cheese.Quantity >= 1.0)
		{
			quantity = GetQuantity(Cheese, max);
			result = new Cheese(quantity);
		}
		return result;
	}

	public void Add(Reagent reagent)
	{
		if (!(reagent.Quantity <= 0.0))
		{
			if (reagent is Flour)
			{
				Flour.Quantity += reagent.Quantity;
			}
			if (reagent is Milk)
			{
				Milk.Quantity += reagent.Quantity;
			}
			if (reagent is Egg)
			{
				Egg.Quantity += reagent.Quantity;
			}
			if (reagent is Iron)
			{
				Iron.Quantity += reagent.Quantity;
			}
			if (reagent is Gold)
			{
				Gold.Quantity += reagent.Quantity;
			}
			if (reagent is Carbon)
			{
				Carbon.Quantity += reagent.Quantity;
			}
			if (reagent is Uranium)
			{
				Uranium.Quantity += reagent.Quantity;
			}
			if (reagent is Copper)
			{
				Copper.Quantity += reagent.Quantity;
			}
			if (reagent is Steel)
			{
				Steel.Quantity += reagent.Quantity;
			}
			if (reagent is Hydrocarbon)
			{
				Hydrocarbon.Quantity += reagent.Quantity;
			}
			if (reagent is Silver)
			{
				Silver.Quantity += reagent.Quantity;
			}
			if (reagent is Nickel)
			{
				Nickel.Quantity += reagent.Quantity;
			}
			if (reagent is Lead)
			{
				Lead.Quantity += reagent.Quantity;
			}
			if (reagent is Electrum)
			{
				Electrum.Quantity += reagent.Quantity;
			}
			if (reagent is Invar)
			{
				Invar.Quantity += reagent.Quantity;
			}
			if (reagent is Constantan)
			{
				Constantan.Quantity += reagent.Quantity;
			}
			if (reagent is Solder)
			{
				Solder.Quantity += reagent.Quantity;
			}
			if (reagent is Plastic)
			{
				Plastic.Quantity += reagent.Quantity;
			}
			if (reagent is Silicon)
			{
				Silicon.Quantity += reagent.Quantity;
			}
			if (reagent is SalicylicAcid)
			{
				SalicylicAcid.Quantity += reagent.Quantity;
			}
			if (reagent is Alcohol)
			{
				Alcohol.Quantity += reagent.Quantity;
			}
			if (reagent is Oil)
			{
				Oil.Quantity += reagent.Quantity;
			}
			if (reagent is Potato)
			{
				Potato.Quantity += reagent.Quantity;
			}
			if (reagent is Tomato)
			{
				Tomato.Quantity += reagent.Quantity;
			}
			if (reagent is Fenoxitone)
			{
				Fenoxitone.Quantity += reagent.Quantity;
			}
			if (reagent is ColorRed)
			{
				ColorRed.Quantity += reagent.Quantity;
			}
			if (reagent is ColorGreen)
			{
				ColorGreen.Quantity += reagent.Quantity;
			}
			if (reagent is ColorBlue)
			{
				ColorBlue.Quantity += reagent.Quantity;
			}
			if (reagent is ColorYellow)
			{
				ColorYellow.Quantity += reagent.Quantity;
			}
			if (reagent is ColorOrange)
			{
				ColorOrange.Quantity += reagent.Quantity;
			}
			if (reagent is Pumpkin)
			{
				Pumpkin.Quantity += reagent.Quantity;
			}
			if (reagent is Rice)
			{
				Rice.Quantity += reagent.Quantity;
			}
			if (reagent is Waspaloy)
			{
				Waspaloy.Quantity += reagent.Quantity;
			}
			if (reagent is Stellite)
			{
				Stellite.Quantity += reagent.Quantity;
			}
			if (reagent is Inconel)
			{
				Inconel.Quantity += reagent.Quantity;
			}
			if (reagent is Hastelloy)
			{
				Hastelloy.Quantity += reagent.Quantity;
			}
			if (reagent is Astroloy)
			{
				Astroloy.Quantity += reagent.Quantity;
			}
			if (reagent is Cobalt)
			{
				Cobalt.Quantity += reagent.Quantity;
			}
			if (reagent is Corn)
			{
				Corn.Quantity += reagent.Quantity;
			}
			if (reagent is Wheat)
			{
				Wheat.Quantity += reagent.Quantity;
			}
			if (reagent is Biomass)
			{
				Biomass.Quantity += reagent.Quantity;
			}
			if (reagent is Soy)
			{
				Soy.Quantity += reagent.Quantity;
			}
			if (reagent is Mushroom)
			{
				Mushroom.Quantity += reagent.Quantity;
			}
			if (reagent is Sugar)
			{
				Sugar.Quantity += reagent.Quantity;
			}
			if (reagent is Cocoa)
			{
				Cocoa.Quantity += reagent.Quantity;
			}
			if (reagent is Cheese)
			{
				Cheese.Quantity += reagent.Quantity;
			}
		}
	}

	public double Get(Reagent reagent)
	{
		if (reagent is Flour)
		{
			return Flour.Quantity;
		}
		if (reagent is Milk)
		{
			return Milk.Quantity;
		}
		if (reagent is Egg)
		{
			return Egg.Quantity;
		}
		if (reagent is Iron)
		{
			return Iron.Quantity;
		}
		if (reagent is Gold)
		{
			return Gold.Quantity;
		}
		if (reagent is Carbon)
		{
			return Carbon.Quantity;
		}
		if (reagent is Uranium)
		{
			return Uranium.Quantity;
		}
		if (reagent is Copper)
		{
			return Copper.Quantity;
		}
		if (reagent is Steel)
		{
			return Steel.Quantity;
		}
		if (reagent is Hydrocarbon)
		{
			return Hydrocarbon.Quantity;
		}
		if (reagent is Silver)
		{
			return Silver.Quantity;
		}
		if (reagent is Nickel)
		{
			return Nickel.Quantity;
		}
		if (reagent is Lead)
		{
			return Lead.Quantity;
		}
		if (reagent is Electrum)
		{
			return Electrum.Quantity;
		}
		if (reagent is Invar)
		{
			return Invar.Quantity;
		}
		if (reagent is Constantan)
		{
			return Constantan.Quantity;
		}
		if (reagent is Solder)
		{
			return Solder.Quantity;
		}
		if (reagent is Plastic)
		{
			return Plastic.Quantity;
		}
		if (reagent is Silicon)
		{
			return Silicon.Quantity;
		}
		if (reagent is SalicylicAcid)
		{
			return SalicylicAcid.Quantity;
		}
		if (reagent is Alcohol)
		{
			return Alcohol.Quantity;
		}
		if (reagent is Oil)
		{
			return Oil.Quantity;
		}
		if (reagent is Potato)
		{
			return Potato.Quantity;
		}
		if (reagent is Tomato)
		{
			return Tomato.Quantity;
		}
		if (reagent is Fenoxitone)
		{
			return Fenoxitone.Quantity;
		}
		if (reagent is ColorRed)
		{
			return ColorRed.Quantity;
		}
		if (reagent is ColorGreen)
		{
			return ColorGreen.Quantity;
		}
		if (reagent is ColorBlue)
		{
			return ColorBlue.Quantity;
		}
		if (reagent is ColorYellow)
		{
			return ColorYellow.Quantity;
		}
		if (reagent is ColorOrange)
		{
			return ColorOrange.Quantity;
		}
		if (reagent is Pumpkin)
		{
			return Pumpkin.Quantity;
		}
		if (reagent is Rice)
		{
			return Rice.Quantity;
		}
		if (reagent is Waspaloy)
		{
			return Waspaloy.Quantity;
		}
		if (reagent is Stellite)
		{
			return Stellite.Quantity;
		}
		if (reagent is Inconel)
		{
			return Inconel.Quantity;
		}
		if (reagent is Hastelloy)
		{
			return Hastelloy.Quantity;
		}
		if (reagent is Astroloy)
		{
			return Astroloy.Quantity;
		}
		if (reagent is Cobalt)
		{
			return Cobalt.Quantity;
		}
		if (reagent is Corn)
		{
			return Corn.Quantity;
		}
		if (reagent is Wheat)
		{
			return Wheat.Quantity;
		}
		if (reagent is Biomass)
		{
			return Biomass.Quantity;
		}
		if (reagent is Soy)
		{
			return Soy.Quantity;
		}
		if (reagent is Mushroom)
		{
			return Mushroom.Quantity;
		}
		if (reagent is Sugar)
		{
			return Sugar.Quantity;
		}
		if (reagent is Cocoa)
		{
			return Cocoa.Quantity;
		}
		if (reagent is Cheese)
		{
			return Cheese.Quantity;
		}
		return 0.0;
	}

	public void Add(Recipe recipe)
	{
		if (recipe.Flour > 0.0)
		{
			Flour.Quantity += recipe.Flour;
		}
		if (recipe.Milk > 0.0)
		{
			Milk.Quantity += recipe.Milk;
		}
		if (recipe.Egg > 0.0)
		{
			Egg.Quantity += recipe.Egg;
		}
		if (recipe.Iron > 0.0)
		{
			Iron.Quantity += recipe.Iron;
		}
		if (recipe.Gold > 0.0)
		{
			Gold.Quantity += recipe.Gold;
		}
		if (recipe.Carbon > 0.0)
		{
			Carbon.Quantity += recipe.Carbon;
		}
		if (recipe.Uranium > 0.0)
		{
			Uranium.Quantity += recipe.Uranium;
		}
		if (recipe.Copper > 0.0)
		{
			Copper.Quantity += recipe.Copper;
		}
		if (recipe.Steel > 0.0)
		{
			Steel.Quantity += recipe.Steel;
		}
		if (recipe.Hydrocarbon > 0.0)
		{
			Hydrocarbon.Quantity += recipe.Hydrocarbon;
		}
		if (recipe.Silver > 0.0)
		{
			Silver.Quantity += recipe.Silver;
		}
		if (recipe.Nickel > 0.0)
		{
			Nickel.Quantity += recipe.Nickel;
		}
		if (recipe.Lead > 0.0)
		{
			Lead.Quantity += recipe.Lead;
		}
		if (recipe.Electrum > 0.0)
		{
			Electrum.Quantity += recipe.Electrum;
		}
		if (recipe.Invar > 0.0)
		{
			Invar.Quantity += recipe.Invar;
		}
		if (recipe.Constantan > 0.0)
		{
			Constantan.Quantity += recipe.Constantan;
		}
		if (recipe.Solder > 0.0)
		{
			Solder.Quantity += recipe.Solder;
		}
		if (recipe.Plastic > 0.0)
		{
			Plastic.Quantity += recipe.Plastic;
		}
		if (recipe.Silicon > 0.0)
		{
			Silicon.Quantity += recipe.Silicon;
		}
		if (recipe.SalicylicAcid > 0.0)
		{
			SalicylicAcid.Quantity += recipe.SalicylicAcid;
		}
		if (recipe.Alcohol > 0.0)
		{
			Alcohol.Quantity += recipe.Alcohol;
		}
		if (recipe.Oil > 0.0)
		{
			Oil.Quantity += recipe.Oil;
		}
		if (recipe.Potato > 0.0)
		{
			Potato.Quantity += recipe.Potato;
		}
		if (recipe.Tomato > 0.0)
		{
			Tomato.Quantity += recipe.Tomato;
		}
		if (recipe.Fenoxitone > 0.0)
		{
			Fenoxitone.Quantity += recipe.Fenoxitone;
		}
		if (recipe.ColorRed > 0.0)
		{
			ColorRed.Quantity += recipe.ColorRed;
		}
		if (recipe.ColorGreen > 0.0)
		{
			ColorGreen.Quantity += recipe.ColorGreen;
		}
		if (recipe.ColorBlue > 0.0)
		{
			ColorBlue.Quantity += recipe.ColorBlue;
		}
		if (recipe.ColorYellow > 0.0)
		{
			ColorYellow.Quantity += recipe.ColorYellow;
		}
		if (recipe.ColorOrange > 0.0)
		{
			ColorOrange.Quantity += recipe.ColorOrange;
		}
		if (recipe.Pumpkin > 0.0)
		{
			Pumpkin.Quantity += recipe.Pumpkin;
		}
		if (recipe.Rice > 0.0)
		{
			Rice.Quantity += recipe.Rice;
		}
		if (recipe.Waspaloy > 0.0)
		{
			Waspaloy.Quantity += recipe.Waspaloy;
		}
		if (recipe.Stellite > 0.0)
		{
			Stellite.Quantity += recipe.Stellite;
		}
		if (recipe.Inconel > 0.0)
		{
			Inconel.Quantity += recipe.Inconel;
		}
		if (recipe.Hastelloy > 0.0)
		{
			Hastelloy.Quantity += recipe.Hastelloy;
		}
		if (recipe.Astroloy > 0.0)
		{
			Astroloy.Quantity += recipe.Astroloy;
		}
		if (recipe.Cobalt > 0.0)
		{
			Cobalt.Quantity += recipe.Cobalt;
		}
		if (recipe.Corn > 0.0)
		{
			Corn.Quantity += recipe.Corn;
		}
		if (recipe.Wheat > 0.0)
		{
			Wheat.Quantity += recipe.Wheat;
		}
		if (recipe.Biomass > 0.0)
		{
			Biomass.Quantity += recipe.Biomass;
		}
		if (recipe.Soy > 0.0)
		{
			Soy.Quantity += recipe.Soy;
		}
		if (recipe.Mushroom > 0.0)
		{
			Mushroom.Quantity += recipe.Mushroom;
		}
		if (recipe.Sugar > 0.0)
		{
			Sugar.Quantity += recipe.Sugar;
		}
		if (recipe.Cocoa > 0.0)
		{
			Cocoa.Quantity += recipe.Cocoa;
		}
		if (recipe.Cheese > 0.0)
		{
			Cheese.Quantity += recipe.Cheese;
		}
	}

	public void Subtract(Reagent reagent)
	{
		if (!(reagent.Quantity <= 0.0))
		{
			if (reagent is Flour)
			{
				Flour.Quantity -= reagent.Quantity;
			}
			if (reagent is Milk)
			{
				Milk.Quantity -= reagent.Quantity;
			}
			if (reagent is Egg)
			{
				Egg.Quantity -= reagent.Quantity;
			}
			if (reagent is Iron)
			{
				Iron.Quantity -= reagent.Quantity;
			}
			if (reagent is Gold)
			{
				Gold.Quantity -= reagent.Quantity;
			}
			if (reagent is Carbon)
			{
				Carbon.Quantity -= reagent.Quantity;
			}
			if (reagent is Uranium)
			{
				Uranium.Quantity -= reagent.Quantity;
			}
			if (reagent is Copper)
			{
				Copper.Quantity -= reagent.Quantity;
			}
			if (reagent is Steel)
			{
				Steel.Quantity -= reagent.Quantity;
			}
			if (reagent is Hydrocarbon)
			{
				Hydrocarbon.Quantity -= reagent.Quantity;
			}
			if (reagent is Silver)
			{
				Silver.Quantity -= reagent.Quantity;
			}
			if (reagent is Nickel)
			{
				Nickel.Quantity -= reagent.Quantity;
			}
			if (reagent is Lead)
			{
				Lead.Quantity -= reagent.Quantity;
			}
			if (reagent is Electrum)
			{
				Electrum.Quantity -= reagent.Quantity;
			}
			if (reagent is Invar)
			{
				Invar.Quantity -= reagent.Quantity;
			}
			if (reagent is Constantan)
			{
				Constantan.Quantity -= reagent.Quantity;
			}
			if (reagent is Solder)
			{
				Solder.Quantity -= reagent.Quantity;
			}
			if (reagent is Plastic)
			{
				Plastic.Quantity -= reagent.Quantity;
			}
			if (reagent is Silicon)
			{
				Silicon.Quantity -= reagent.Quantity;
			}
			if (reagent is SalicylicAcid)
			{
				SalicylicAcid.Quantity -= reagent.Quantity;
			}
			if (reagent is Alcohol)
			{
				Alcohol.Quantity -= reagent.Quantity;
			}
			if (reagent is Oil)
			{
				Oil.Quantity -= reagent.Quantity;
			}
			if (reagent is Potato)
			{
				Potato.Quantity -= reagent.Quantity;
			}
			if (reagent is Tomato)
			{
				Tomato.Quantity -= reagent.Quantity;
			}
			if (reagent is Fenoxitone)
			{
				Fenoxitone.Quantity -= reagent.Quantity;
			}
			if (reagent is ColorRed)
			{
				ColorRed.Quantity -= reagent.Quantity;
			}
			if (reagent is ColorGreen)
			{
				ColorGreen.Quantity -= reagent.Quantity;
			}
			if (reagent is ColorBlue)
			{
				ColorBlue.Quantity -= reagent.Quantity;
			}
			if (reagent is ColorYellow)
			{
				ColorYellow.Quantity -= reagent.Quantity;
			}
			if (reagent is ColorOrange)
			{
				ColorOrange.Quantity -= reagent.Quantity;
			}
			if (reagent is Pumpkin)
			{
				Pumpkin.Quantity -= reagent.Quantity;
			}
			if (reagent is Rice)
			{
				Rice.Quantity -= reagent.Quantity;
			}
			if (reagent is Waspaloy)
			{
				Waspaloy.Quantity -= reagent.Quantity;
			}
			if (reagent is Stellite)
			{
				Stellite.Quantity -= reagent.Quantity;
			}
			if (reagent is Inconel)
			{
				Inconel.Quantity -= reagent.Quantity;
			}
			if (reagent is Hastelloy)
			{
				Hastelloy.Quantity -= reagent.Quantity;
			}
			if (reagent is Astroloy)
			{
				Astroloy.Quantity -= reagent.Quantity;
			}
			if (reagent is Cobalt)
			{
				Cobalt.Quantity -= reagent.Quantity;
			}
			if (reagent is Corn)
			{
				Corn.Quantity -= reagent.Quantity;
			}
			if (reagent is Wheat)
			{
				Wheat.Quantity -= reagent.Quantity;
			}
			if (reagent is Biomass)
			{
				Biomass.Quantity -= reagent.Quantity;
			}
			if (reagent is Soy)
			{
				Soy.Quantity -= reagent.Quantity;
			}
			if (reagent is Mushroom)
			{
				Mushroom.Quantity -= reagent.Quantity;
			}
			if (reagent is Sugar)
			{
				Sugar.Quantity -= reagent.Quantity;
			}
			if (reagent is Cocoa)
			{
				Cocoa.Quantity -= reagent.Quantity;
			}
			if (reagent is Cheese)
			{
				Cheese.Quantity -= reagent.Quantity;
			}
		}
	}

	public void Subtract(Recipe recipe, float subtractMultiplier = 1f)
	{
		if (recipe.Flour > 0.0)
		{
			Flour.Quantity -= recipe.Flour * (double)subtractMultiplier;
		}
		if (recipe.Milk > 0.0)
		{
			Milk.Quantity -= recipe.Milk * (double)subtractMultiplier;
		}
		if (recipe.Egg > 0.0)
		{
			Egg.Quantity -= recipe.Egg * (double)subtractMultiplier;
		}
		if (recipe.Iron > 0.0)
		{
			Iron.Quantity -= recipe.Iron * (double)subtractMultiplier;
		}
		if (recipe.Gold > 0.0)
		{
			Gold.Quantity -= recipe.Gold * (double)subtractMultiplier;
		}
		if (recipe.Carbon > 0.0)
		{
			Carbon.Quantity -= recipe.Carbon * (double)subtractMultiplier;
		}
		if (recipe.Uranium > 0.0)
		{
			Uranium.Quantity -= recipe.Uranium * (double)subtractMultiplier;
		}
		if (recipe.Copper > 0.0)
		{
			Copper.Quantity -= recipe.Copper * (double)subtractMultiplier;
		}
		if (recipe.Steel > 0.0)
		{
			Steel.Quantity -= recipe.Steel * (double)subtractMultiplier;
		}
		if (recipe.Hydrocarbon > 0.0)
		{
			Hydrocarbon.Quantity -= recipe.Hydrocarbon * (double)subtractMultiplier;
		}
		if (recipe.Silver > 0.0)
		{
			Silver.Quantity -= recipe.Silver * (double)subtractMultiplier;
		}
		if (recipe.Nickel > 0.0)
		{
			Nickel.Quantity -= recipe.Nickel * (double)subtractMultiplier;
		}
		if (recipe.Lead > 0.0)
		{
			Lead.Quantity -= recipe.Lead * (double)subtractMultiplier;
		}
		if (recipe.Electrum > 0.0)
		{
			Electrum.Quantity -= recipe.Electrum * (double)subtractMultiplier;
		}
		if (recipe.Invar > 0.0)
		{
			Invar.Quantity -= recipe.Invar * (double)subtractMultiplier;
		}
		if (recipe.Constantan > 0.0)
		{
			Constantan.Quantity -= recipe.Constantan * (double)subtractMultiplier;
		}
		if (recipe.Solder > 0.0)
		{
			Solder.Quantity -= recipe.Solder * (double)subtractMultiplier;
		}
		if (recipe.Plastic > 0.0)
		{
			Plastic.Quantity -= recipe.Plastic * (double)subtractMultiplier;
		}
		if (recipe.Silicon > 0.0)
		{
			Silicon.Quantity -= recipe.Silicon * (double)subtractMultiplier;
		}
		if (recipe.SalicylicAcid > 0.0)
		{
			SalicylicAcid.Quantity -= recipe.SalicylicAcid * (double)subtractMultiplier;
		}
		if (recipe.Alcohol > 0.0)
		{
			Alcohol.Quantity -= recipe.Alcohol * (double)subtractMultiplier;
		}
		if (recipe.Oil > 0.0)
		{
			Oil.Quantity -= recipe.Oil * (double)subtractMultiplier;
		}
		if (recipe.Potato > 0.0)
		{
			Potato.Quantity -= recipe.Potato * (double)subtractMultiplier;
		}
		if (recipe.Tomato > 0.0)
		{
			Tomato.Quantity -= recipe.Tomato * (double)subtractMultiplier;
		}
		if (recipe.Fenoxitone > 0.0)
		{
			Fenoxitone.Quantity -= recipe.Fenoxitone * (double)subtractMultiplier;
		}
		if (recipe.ColorRed > 0.0)
		{
			ColorRed.Quantity -= recipe.ColorRed * (double)subtractMultiplier;
		}
		if (recipe.ColorGreen > 0.0)
		{
			ColorGreen.Quantity -= recipe.ColorGreen * (double)subtractMultiplier;
		}
		if (recipe.ColorBlue > 0.0)
		{
			ColorBlue.Quantity -= recipe.ColorBlue * (double)subtractMultiplier;
		}
		if (recipe.ColorYellow > 0.0)
		{
			ColorYellow.Quantity -= recipe.ColorYellow * (double)subtractMultiplier;
		}
		if (recipe.ColorOrange > 0.0)
		{
			ColorOrange.Quantity -= recipe.ColorOrange * (double)subtractMultiplier;
		}
		if (recipe.Pumpkin > 0.0)
		{
			Pumpkin.Quantity -= recipe.Pumpkin * (double)subtractMultiplier;
		}
		if (recipe.Rice > 0.0)
		{
			Rice.Quantity -= recipe.Rice * (double)subtractMultiplier;
		}
		if (recipe.Waspaloy > 0.0)
		{
			Waspaloy.Quantity -= recipe.Waspaloy * (double)subtractMultiplier;
		}
		if (recipe.Stellite > 0.0)
		{
			Stellite.Quantity -= recipe.Stellite * (double)subtractMultiplier;
		}
		if (recipe.Inconel > 0.0)
		{
			Inconel.Quantity -= recipe.Inconel * (double)subtractMultiplier;
		}
		if (recipe.Hastelloy > 0.0)
		{
			Hastelloy.Quantity -= recipe.Hastelloy * (double)subtractMultiplier;
		}
		if (recipe.Astroloy > 0.0)
		{
			Astroloy.Quantity -= recipe.Astroloy * (double)subtractMultiplier;
		}
		if (recipe.Cobalt > 0.0)
		{
			Cobalt.Quantity -= recipe.Cobalt * (double)subtractMultiplier;
		}
		if (recipe.Corn > 0.0)
		{
			Corn.Quantity -= recipe.Corn * (double)subtractMultiplier;
		}
		if (recipe.Wheat > 0.0)
		{
			Wheat.Quantity -= recipe.Wheat * (double)subtractMultiplier;
		}
		if (recipe.Biomass > 0.0)
		{
			Biomass.Quantity -= recipe.Biomass * (double)subtractMultiplier;
		}
		if (recipe.Soy > 0.0)
		{
			Soy.Quantity -= recipe.Soy * (double)subtractMultiplier;
		}
		if (recipe.Mushroom > 0.0)
		{
			Mushroom.Quantity -= recipe.Mushroom * (double)subtractMultiplier;
		}
		if (recipe.Sugar > 0.0)
		{
			Sugar.Quantity -= recipe.Sugar * (double)subtractMultiplier;
		}
		if (recipe.Cocoa > 0.0)
		{
			Cocoa.Quantity -= recipe.Cocoa * (double)subtractMultiplier;
		}
		if (recipe.Cheese > 0.0)
		{
			Cheese.Quantity -= recipe.Cheese * (double)subtractMultiplier;
		}
	}

	public void Add(ReagentMixture reagentMixture)
	{
		Add(reagentMixture.Flour);
		Add(reagentMixture.Milk);
		Add(reagentMixture.Egg);
		Add(reagentMixture.Iron);
		Add(reagentMixture.Gold);
		Add(reagentMixture.Carbon);
		Add(reagentMixture.Uranium);
		Add(reagentMixture.Copper);
		Add(reagentMixture.Steel);
		Add(reagentMixture.Hydrocarbon);
		Add(reagentMixture.Silver);
		Add(reagentMixture.Nickel);
		Add(reagentMixture.Lead);
		Add(reagentMixture.Electrum);
		Add(reagentMixture.Invar);
		Add(reagentMixture.Constantan);
		Add(reagentMixture.Solder);
		Add(reagentMixture.Plastic);
		Add(reagentMixture.Silicon);
		Add(reagentMixture.SalicylicAcid);
		Add(reagentMixture.Alcohol);
		Add(reagentMixture.Oil);
		Add(reagentMixture.Potato);
		Add(reagentMixture.Tomato);
		Add(reagentMixture.Fenoxitone);
		Add(reagentMixture.ColorRed);
		Add(reagentMixture.ColorGreen);
		Add(reagentMixture.ColorBlue);
		Add(reagentMixture.ColorYellow);
		Add(reagentMixture.ColorOrange);
		Add(reagentMixture.Pumpkin);
		Add(reagentMixture.Rice);
		Add(reagentMixture.Waspaloy);
		Add(reagentMixture.Stellite);
		Add(reagentMixture.Inconel);
		Add(reagentMixture.Hastelloy);
		Add(reagentMixture.Astroloy);
		Add(reagentMixture.Cobalt);
		Add(reagentMixture.Corn);
		Add(reagentMixture.Wheat);
		Add(reagentMixture.Biomass);
		Add(reagentMixture.Soy);
		Add(reagentMixture.Mushroom);
		Add(reagentMixture.Sugar);
		Add(reagentMixture.Cocoa);
		Add(reagentMixture.Cheese);
	}

	public void Subtract(ReagentMixture reagentMixture)
	{
		Subtract(reagentMixture.Flour);
		Subtract(reagentMixture.Milk);
		Subtract(reagentMixture.Egg);
		Subtract(reagentMixture.Iron);
		Subtract(reagentMixture.Gold);
		Subtract(reagentMixture.Carbon);
		Subtract(reagentMixture.Uranium);
		Subtract(reagentMixture.Copper);
		Subtract(reagentMixture.Steel);
		Subtract(reagentMixture.Hydrocarbon);
		Subtract(reagentMixture.Silver);
		Subtract(reagentMixture.Nickel);
		Subtract(reagentMixture.Lead);
		Subtract(reagentMixture.Electrum);
		Subtract(reagentMixture.Invar);
		Subtract(reagentMixture.Constantan);
		Subtract(reagentMixture.Solder);
		Subtract(reagentMixture.Plastic);
		Subtract(reagentMixture.Silicon);
		Subtract(reagentMixture.SalicylicAcid);
		Subtract(reagentMixture.Alcohol);
		Subtract(reagentMixture.Oil);
		Subtract(reagentMixture.Potato);
		Subtract(reagentMixture.Tomato);
		Subtract(reagentMixture.Fenoxitone);
		Subtract(reagentMixture.ColorRed);
		Subtract(reagentMixture.ColorGreen);
		Subtract(reagentMixture.ColorBlue);
		Subtract(reagentMixture.ColorYellow);
		Subtract(reagentMixture.ColorOrange);
		Subtract(reagentMixture.Pumpkin);
		Subtract(reagentMixture.Rice);
		Subtract(reagentMixture.Waspaloy);
		Subtract(reagentMixture.Stellite);
		Subtract(reagentMixture.Inconel);
		Subtract(reagentMixture.Hastelloy);
		Subtract(reagentMixture.Astroloy);
		Subtract(reagentMixture.Cobalt);
		Subtract(reagentMixture.Corn);
		Subtract(reagentMixture.Wheat);
		Subtract(reagentMixture.Biomass);
		Subtract(reagentMixture.Soy);
		Subtract(reagentMixture.Mushroom);
		Subtract(reagentMixture.Sugar);
		Subtract(reagentMixture.Cocoa);
		Subtract(reagentMixture.Cheese);
	}

	public static ReagentMixture operator /(ReagentMixture left, double right)
	{
		left.Flour.Quantity = Math.Round(left.Flour.Quantity / right, 6);
		left.Milk.Quantity = Math.Round(left.Milk.Quantity / right, 6);
		left.Egg.Quantity = Math.Round(left.Egg.Quantity / right, 6);
		left.Iron.Quantity = Math.Round(left.Iron.Quantity / right, 6);
		left.Gold.Quantity = Math.Round(left.Gold.Quantity / right, 6);
		left.Carbon.Quantity = Math.Round(left.Carbon.Quantity / right, 6);
		left.Uranium.Quantity = Math.Round(left.Uranium.Quantity / right, 6);
		left.Copper.Quantity = Math.Round(left.Copper.Quantity / right, 6);
		left.Steel.Quantity = Math.Round(left.Steel.Quantity / right, 6);
		left.Hydrocarbon.Quantity = Math.Round(left.Hydrocarbon.Quantity / right, 6);
		left.Silver.Quantity = Math.Round(left.Silver.Quantity / right, 6);
		left.Nickel.Quantity = Math.Round(left.Nickel.Quantity / right, 6);
		left.Lead.Quantity = Math.Round(left.Lead.Quantity / right, 6);
		left.Electrum.Quantity = Math.Round(left.Electrum.Quantity / right, 6);
		left.Invar.Quantity = Math.Round(left.Invar.Quantity / right, 6);
		left.Constantan.Quantity = Math.Round(left.Constantan.Quantity / right, 6);
		left.Solder.Quantity = Math.Round(left.Solder.Quantity / right, 6);
		left.Plastic.Quantity = Math.Round(left.Plastic.Quantity / right, 6);
		left.Silicon.Quantity = Math.Round(left.Silicon.Quantity / right, 6);
		left.SalicylicAcid.Quantity = Math.Round(left.SalicylicAcid.Quantity / right, 6);
		left.Alcohol.Quantity = Math.Round(left.Alcohol.Quantity / right, 6);
		left.Oil.Quantity = Math.Round(left.Oil.Quantity / right, 6);
		left.Potato.Quantity = Math.Round(left.Potato.Quantity / right, 6);
		left.Tomato.Quantity = Math.Round(left.Tomato.Quantity / right, 6);
		left.Fenoxitone.Quantity = Math.Round(left.Fenoxitone.Quantity / right, 6);
		left.ColorRed.Quantity = Math.Round(left.ColorRed.Quantity / right, 6);
		left.ColorGreen.Quantity = Math.Round(left.ColorGreen.Quantity / right, 6);
		left.ColorBlue.Quantity = Math.Round(left.ColorBlue.Quantity / right, 6);
		left.ColorYellow.Quantity = Math.Round(left.ColorYellow.Quantity / right, 6);
		left.ColorOrange.Quantity = Math.Round(left.ColorOrange.Quantity / right, 6);
		left.Pumpkin.Quantity = Math.Round(left.Pumpkin.Quantity / right, 6);
		left.Rice.Quantity = Math.Round(left.Rice.Quantity / right, 6);
		left.Waspaloy.Quantity = Math.Round(left.Waspaloy.Quantity / right, 6);
		left.Stellite.Quantity = Math.Round(left.Stellite.Quantity / right, 6);
		left.Inconel.Quantity = Math.Round(left.Inconel.Quantity / right, 6);
		left.Hastelloy.Quantity = Math.Round(left.Hastelloy.Quantity / right, 6);
		left.Astroloy.Quantity = Math.Round(left.Astroloy.Quantity / right, 6);
		left.Cobalt.Quantity = Math.Round(left.Cobalt.Quantity / right, 6);
		left.Corn.Quantity = Math.Round(left.Corn.Quantity / right, 6);
		left.Wheat.Quantity = Math.Round(left.Wheat.Quantity / right, 6);
		left.Biomass.Quantity = Math.Round(left.Biomass.Quantity / right, 6);
		left.Soy.Quantity = Math.Round(left.Soy.Quantity / right, 6);
		left.Mushroom.Quantity = Math.Round(left.Mushroom.Quantity / right, 6);
		left.Sugar.Quantity = Math.Round(left.Sugar.Quantity / right, 6);
		left.Cocoa.Quantity = Math.Round(left.Cocoa.Quantity / right, 6);
		left.Cheese.Quantity = Math.Round(left.Cheese.Quantity / right, 6);
		return left;
	}

	public static ReagentMixture operator *(ReagentMixture left, double right)
	{
		left.Flour.Quantity *= right;
		left.Milk.Quantity *= right;
		left.Egg.Quantity *= right;
		left.Iron.Quantity *= right;
		left.Gold.Quantity *= right;
		left.Carbon.Quantity *= right;
		left.Uranium.Quantity *= right;
		left.Copper.Quantity *= right;
		left.Steel.Quantity *= right;
		left.Hydrocarbon.Quantity *= right;
		left.Silver.Quantity *= right;
		left.Nickel.Quantity *= right;
		left.Lead.Quantity *= right;
		left.Electrum.Quantity *= right;
		left.Invar.Quantity *= right;
		left.Constantan.Quantity *= right;
		left.Solder.Quantity *= right;
		left.Plastic.Quantity *= right;
		left.Silicon.Quantity *= right;
		left.SalicylicAcid.Quantity *= right;
		left.Alcohol.Quantity *= right;
		left.Oil.Quantity *= right;
		left.Potato.Quantity *= right;
		left.Tomato.Quantity *= right;
		left.Fenoxitone.Quantity *= right;
		left.ColorRed.Quantity *= right;
		left.ColorGreen.Quantity *= right;
		left.ColorBlue.Quantity *= right;
		left.ColorYellow.Quantity *= right;
		left.ColorOrange.Quantity *= right;
		left.Pumpkin.Quantity *= right;
		left.Rice.Quantity *= right;
		left.Waspaloy.Quantity *= right;
		left.Stellite.Quantity *= right;
		left.Inconel.Quantity *= right;
		left.Hastelloy.Quantity *= right;
		left.Astroloy.Quantity *= right;
		left.Cobalt.Quantity *= right;
		left.Corn.Quantity *= right;
		left.Wheat.Quantity *= right;
		left.Biomass.Quantity *= right;
		left.Soy.Quantity *= right;
		left.Mushroom.Quantity *= right;
		left.Sugar.Quantity *= right;
		left.Cocoa.Quantity *= right;
		left.Cheese.Quantity *= right;
		return left;
	}

	public void Set(Reagent reagent)
	{
		if (reagent is Flour)
		{
			Flour.Quantity = reagent.Quantity;
		}
		if (reagent is Milk)
		{
			Milk.Quantity = reagent.Quantity;
		}
		if (reagent is Egg)
		{
			Egg.Quantity = reagent.Quantity;
		}
		if (reagent is Iron)
		{
			Iron.Quantity = reagent.Quantity;
		}
		if (reagent is Gold)
		{
			Gold.Quantity = reagent.Quantity;
		}
		if (reagent is Carbon)
		{
			Carbon.Quantity = reagent.Quantity;
		}
		if (reagent is Uranium)
		{
			Uranium.Quantity = reagent.Quantity;
		}
		if (reagent is Copper)
		{
			Copper.Quantity = reagent.Quantity;
		}
		if (reagent is Steel)
		{
			Steel.Quantity = reagent.Quantity;
		}
		if (reagent is Hydrocarbon)
		{
			Hydrocarbon.Quantity = reagent.Quantity;
		}
		if (reagent is Silver)
		{
			Silver.Quantity = reagent.Quantity;
		}
		if (reagent is Nickel)
		{
			Nickel.Quantity = reagent.Quantity;
		}
		if (reagent is Lead)
		{
			Lead.Quantity = reagent.Quantity;
		}
		if (reagent is Electrum)
		{
			Electrum.Quantity = reagent.Quantity;
		}
		if (reagent is Invar)
		{
			Invar.Quantity = reagent.Quantity;
		}
		if (reagent is Constantan)
		{
			Constantan.Quantity = reagent.Quantity;
		}
		if (reagent is Solder)
		{
			Solder.Quantity = reagent.Quantity;
		}
		if (reagent is Plastic)
		{
			Plastic.Quantity = reagent.Quantity;
		}
		if (reagent is Silicon)
		{
			Silicon.Quantity = reagent.Quantity;
		}
		if (reagent is SalicylicAcid)
		{
			SalicylicAcid.Quantity = reagent.Quantity;
		}
		if (reagent is Alcohol)
		{
			Alcohol.Quantity = reagent.Quantity;
		}
		if (reagent is Oil)
		{
			Oil.Quantity = reagent.Quantity;
		}
		if (reagent is Potato)
		{
			Potato.Quantity = reagent.Quantity;
		}
		if (reagent is Tomato)
		{
			Tomato.Quantity = reagent.Quantity;
		}
		if (reagent is Fenoxitone)
		{
			Fenoxitone.Quantity = reagent.Quantity;
		}
		if (reagent is ColorRed)
		{
			ColorRed.Quantity = reagent.Quantity;
		}
		if (reagent is ColorGreen)
		{
			ColorGreen.Quantity = reagent.Quantity;
		}
		if (reagent is ColorBlue)
		{
			ColorBlue.Quantity = reagent.Quantity;
		}
		if (reagent is ColorYellow)
		{
			ColorYellow.Quantity = reagent.Quantity;
		}
		if (reagent is ColorOrange)
		{
			ColorOrange.Quantity = reagent.Quantity;
		}
		if (reagent is Pumpkin)
		{
			Pumpkin.Quantity = reagent.Quantity;
		}
		if (reagent is Rice)
		{
			Rice.Quantity = reagent.Quantity;
		}
		if (reagent is Waspaloy)
		{
			Waspaloy.Quantity = reagent.Quantity;
		}
		if (reagent is Stellite)
		{
			Stellite.Quantity = reagent.Quantity;
		}
		if (reagent is Inconel)
		{
			Inconel.Quantity = reagent.Quantity;
		}
		if (reagent is Hastelloy)
		{
			Hastelloy.Quantity = reagent.Quantity;
		}
		if (reagent is Astroloy)
		{
			Astroloy.Quantity = reagent.Quantity;
		}
		if (reagent is Cobalt)
		{
			Cobalt.Quantity = reagent.Quantity;
		}
		if (reagent is Corn)
		{
			Corn.Quantity = reagent.Quantity;
		}
		if (reagent is Wheat)
		{
			Wheat.Quantity = reagent.Quantity;
		}
		if (reagent is Biomass)
		{
			Biomass.Quantity = reagent.Quantity;
		}
		if (reagent is Soy)
		{
			Soy.Quantity = reagent.Quantity;
		}
		if (reagent is Mushroom)
		{
			Mushroom.Quantity = reagent.Quantity;
		}
		if (reagent is Sugar)
		{
			Sugar.Quantity = reagent.Quantity;
		}
		if (reagent is Cocoa)
		{
			Cocoa.Quantity = reagent.Quantity;
		}
		if (reagent is Cheese)
		{
			Cheese.Quantity = reagent.Quantity;
		}
	}

	public void Set(byte reagentId, double quantity)
	{
		if (reagentId == 0)
		{
			Flour.Quantity = quantity;
		}
		if (reagentId == 1)
		{
			Milk.Quantity = quantity;
		}
		if (reagentId == 2)
		{
			Egg.Quantity = quantity;
		}
		if (reagentId == 3)
		{
			Iron.Quantity = quantity;
		}
		if (reagentId == 4)
		{
			Gold.Quantity = quantity;
		}
		if (reagentId == 5)
		{
			Carbon.Quantity = quantity;
		}
		if (reagentId == 6)
		{
			Uranium.Quantity = quantity;
		}
		if (reagentId == 7)
		{
			Copper.Quantity = quantity;
		}
		if (reagentId == 8)
		{
			Steel.Quantity = quantity;
		}
		if (reagentId == 9)
		{
			Hydrocarbon.Quantity = quantity;
		}
		if (reagentId == 10)
		{
			Silver.Quantity = quantity;
		}
		if (reagentId == 11)
		{
			Nickel.Quantity = quantity;
		}
		if (reagentId == 12)
		{
			Lead.Quantity = quantity;
		}
		if (reagentId == 13)
		{
			Electrum.Quantity = quantity;
		}
		if (reagentId == 14)
		{
			Invar.Quantity = quantity;
		}
		if (reagentId == 15)
		{
			Constantan.Quantity = quantity;
		}
		if (reagentId == 16)
		{
			Solder.Quantity = quantity;
		}
		if (reagentId == 17)
		{
			Plastic.Quantity = quantity;
		}
		if (reagentId == 18)
		{
			Silicon.Quantity = quantity;
		}
		if (reagentId == 19)
		{
			SalicylicAcid.Quantity = quantity;
		}
		if (reagentId == 20)
		{
			Alcohol.Quantity = quantity;
		}
		if (reagentId == 21)
		{
			Oil.Quantity = quantity;
		}
		if (reagentId == 22)
		{
			Potato.Quantity = quantity;
		}
		if (reagentId == 23)
		{
			Tomato.Quantity = quantity;
		}
		if (reagentId == 24)
		{
			Fenoxitone.Quantity = quantity;
		}
		if (reagentId == 25)
		{
			ColorRed.Quantity = quantity;
		}
		if (reagentId == 26)
		{
			ColorGreen.Quantity = quantity;
		}
		if (reagentId == 27)
		{
			ColorBlue.Quantity = quantity;
		}
		if (reagentId == 28)
		{
			ColorYellow.Quantity = quantity;
		}
		if (reagentId == 29)
		{
			ColorOrange.Quantity = quantity;
		}
		if (reagentId == 30)
		{
			Pumpkin.Quantity = quantity;
		}
		if (reagentId == 31)
		{
			Rice.Quantity = quantity;
		}
		if (reagentId == 32)
		{
			Waspaloy.Quantity = quantity;
		}
		if (reagentId == 33)
		{
			Stellite.Quantity = quantity;
		}
		if (reagentId == 34)
		{
			Inconel.Quantity = quantity;
		}
		if (reagentId == 35)
		{
			Hastelloy.Quantity = quantity;
		}
		if (reagentId == 36)
		{
			Astroloy.Quantity = quantity;
		}
		if (reagentId == 37)
		{
			Cobalt.Quantity = quantity;
		}
		if (reagentId == 38)
		{
			Corn.Quantity = quantity;
		}
		if (reagentId == 39)
		{
			Wheat.Quantity = quantity;
		}
		if (reagentId == 40)
		{
			Biomass.Quantity = quantity;
		}
		if (reagentId == 41)
		{
			Soy.Quantity = quantity;
		}
		if (reagentId == 42)
		{
			Mushroom.Quantity = quantity;
		}
		if (reagentId == 43)
		{
			Sugar.Quantity = quantity;
		}
		if (reagentId == 44)
		{
			Cocoa.Quantity = quantity;
		}
		if (reagentId == 45)
		{
			Cheese.Quantity = quantity;
		}
	}

	public void Set(ReagentMixture reagentMixture)
	{
		Set(reagentMixture.Flour);
		Set(reagentMixture.Milk);
		Set(reagentMixture.Egg);
		Set(reagentMixture.Iron);
		Set(reagentMixture.Gold);
		Set(reagentMixture.Carbon);
		Set(reagentMixture.Uranium);
		Set(reagentMixture.Copper);
		Set(reagentMixture.Steel);
		Set(reagentMixture.Hydrocarbon);
		Set(reagentMixture.Silver);
		Set(reagentMixture.Nickel);
		Set(reagentMixture.Lead);
		Set(reagentMixture.Electrum);
		Set(reagentMixture.Invar);
		Set(reagentMixture.Constantan);
		Set(reagentMixture.Solder);
		Set(reagentMixture.Plastic);
		Set(reagentMixture.Silicon);
		Set(reagentMixture.SalicylicAcid);
		Set(reagentMixture.Alcohol);
		Set(reagentMixture.Oil);
		Set(reagentMixture.Potato);
		Set(reagentMixture.Tomato);
		Set(reagentMixture.Fenoxitone);
		Set(reagentMixture.ColorRed);
		Set(reagentMixture.ColorGreen);
		Set(reagentMixture.ColorBlue);
		Set(reagentMixture.ColorYellow);
		Set(reagentMixture.ColorOrange);
		Set(reagentMixture.Pumpkin);
		Set(reagentMixture.Rice);
		Set(reagentMixture.Waspaloy);
		Set(reagentMixture.Stellite);
		Set(reagentMixture.Inconel);
		Set(reagentMixture.Hastelloy);
		Set(reagentMixture.Astroloy);
		Set(reagentMixture.Cobalt);
		Set(reagentMixture.Corn);
		Set(reagentMixture.Wheat);
		Set(reagentMixture.Biomass);
		Set(reagentMixture.Soy);
		Set(reagentMixture.Mushroom);
		Set(reagentMixture.Sugar);
		Set(reagentMixture.Cocoa);
		Set(reagentMixture.Cheese);
	}

	public override string ToString()
	{
		return ToString(new StringBuilder());
	}

	public string ToString(StringBuilder sb)
	{
		BuildReagentString(sb);
		return sb.ToString();
	}

	public void BuildReagentString(StringBuilder sb)
	{
		MakeReagentList(Flour, sb);
		MakeReagentList(Milk, sb);
		MakeReagentList(Egg, sb);
		MakeReagentList(Iron, sb);
		MakeReagentList(Gold, sb);
		MakeReagentList(Carbon, sb);
		MakeReagentList(Uranium, sb);
		MakeReagentList(Copper, sb);
		MakeReagentList(Steel, sb);
		MakeReagentList(Hydrocarbon, sb);
		MakeReagentList(Silver, sb);
		MakeReagentList(Nickel, sb);
		MakeReagentList(Lead, sb);
		MakeReagentList(Electrum, sb);
		MakeReagentList(Invar, sb);
		MakeReagentList(Constantan, sb);
		MakeReagentList(Solder, sb);
		MakeReagentList(Plastic, sb);
		MakeReagentList(Silicon, sb);
		MakeReagentList(SalicylicAcid, sb);
		MakeReagentList(Alcohol, sb);
		MakeReagentList(Oil, sb);
		MakeReagentList(Potato, sb);
		MakeReagentList(Tomato, sb);
		MakeReagentList(Fenoxitone, sb);
		MakeReagentList(ColorRed, sb);
		MakeReagentList(ColorGreen, sb);
		MakeReagentList(ColorBlue, sb);
		MakeReagentList(ColorYellow, sb);
		MakeReagentList(ColorOrange, sb);
		MakeReagentList(Pumpkin, sb);
		MakeReagentList(Rice, sb);
		MakeReagentList(Waspaloy, sb);
		MakeReagentList(Stellite, sb);
		MakeReagentList(Inconel, sb);
		MakeReagentList(Hastelloy, sb);
		MakeReagentList(Astroloy, sb);
		MakeReagentList(Cobalt, sb);
		MakeReagentList(Corn, sb);
		MakeReagentList(Wheat, sb);
		MakeReagentList(Biomass, sb);
		MakeReagentList(Soy, sb);
		MakeReagentList(Mushroom, sb);
		MakeReagentList(Sugar, sb);
		MakeReagentList(Cocoa, sb);
		MakeReagentList(Cheese, sb);
	}

	public string ToString(float scale)
	{
		StringBuilder stringBuilder = new StringBuilder();
		MakeReagentList(Flour, stringBuilder, scale);
		MakeReagentList(Milk, stringBuilder, scale);
		MakeReagentList(Egg, stringBuilder, scale);
		MakeReagentList(Iron, stringBuilder, scale);
		MakeReagentList(Gold, stringBuilder, scale);
		MakeReagentList(Carbon, stringBuilder, scale);
		MakeReagentList(Uranium, stringBuilder, scale);
		MakeReagentList(Copper, stringBuilder, scale);
		MakeReagentList(Steel, stringBuilder, scale);
		MakeReagentList(Hydrocarbon, stringBuilder, scale);
		MakeReagentList(Silver, stringBuilder, scale);
		MakeReagentList(Nickel, stringBuilder, scale);
		MakeReagentList(Lead, stringBuilder, scale);
		MakeReagentList(Electrum, stringBuilder, scale);
		MakeReagentList(Invar, stringBuilder, scale);
		MakeReagentList(Constantan, stringBuilder, scale);
		MakeReagentList(Solder, stringBuilder, scale);
		MakeReagentList(Plastic, stringBuilder, scale);
		MakeReagentList(Silicon, stringBuilder, scale);
		MakeReagentList(SalicylicAcid, stringBuilder, scale);
		MakeReagentList(Alcohol, stringBuilder, scale);
		MakeReagentList(Oil, stringBuilder, scale);
		MakeReagentList(Potato, stringBuilder, scale);
		MakeReagentList(Tomato, stringBuilder, scale);
		MakeReagentList(Fenoxitone, stringBuilder, scale);
		MakeReagentList(ColorRed, stringBuilder, scale);
		MakeReagentList(ColorGreen, stringBuilder, scale);
		MakeReagentList(ColorBlue, stringBuilder, scale);
		MakeReagentList(ColorYellow, stringBuilder, scale);
		MakeReagentList(ColorOrange, stringBuilder, scale);
		MakeReagentList(Pumpkin, stringBuilder, scale);
		MakeReagentList(Rice, stringBuilder, scale);
		MakeReagentList(Waspaloy, stringBuilder, scale);
		MakeReagentList(Stellite, stringBuilder, scale);
		MakeReagentList(Inconel, stringBuilder, scale);
		MakeReagentList(Hastelloy, stringBuilder, scale);
		MakeReagentList(Astroloy, stringBuilder, scale);
		MakeReagentList(Cobalt, stringBuilder, scale);
		MakeReagentList(Corn, stringBuilder, scale);
		MakeReagentList(Wheat, stringBuilder, scale);
		MakeReagentList(Biomass, stringBuilder, scale);
		MakeReagentList(Soy, stringBuilder, scale);
		MakeReagentList(Mushroom, stringBuilder, scale);
		MakeReagentList(Sugar, stringBuilder, scale);
		MakeReagentList(Cocoa, stringBuilder, scale);
		MakeReagentList(Cheese, stringBuilder, scale);
		return stringBuilder.ToString();
	}

	public List<StationFoundInInsert> ToStationpediaString()
	{
		List<StationFoundInInsert> insertList = new List<StationFoundInInsert>();
		MakeReagentList(Flour, ref insertList);
		MakeReagentList(Milk, ref insertList);
		MakeReagentList(Egg, ref insertList);
		MakeReagentList(Iron, ref insertList);
		MakeReagentList(Gold, ref insertList);
		MakeReagentList(Carbon, ref insertList);
		MakeReagentList(Uranium, ref insertList);
		MakeReagentList(Copper, ref insertList);
		MakeReagentList(Steel, ref insertList);
		MakeReagentList(Hydrocarbon, ref insertList);
		MakeReagentList(Silver, ref insertList);
		MakeReagentList(Nickel, ref insertList);
		MakeReagentList(Lead, ref insertList);
		MakeReagentList(Electrum, ref insertList);
		MakeReagentList(Invar, ref insertList);
		MakeReagentList(Constantan, ref insertList);
		MakeReagentList(Solder, ref insertList);
		MakeReagentList(Plastic, ref insertList);
		MakeReagentList(Silicon, ref insertList);
		MakeReagentList(SalicylicAcid, ref insertList);
		MakeReagentList(Alcohol, ref insertList);
		MakeReagentList(Oil, ref insertList);
		MakeReagentList(Potato, ref insertList);
		MakeReagentList(Tomato, ref insertList);
		MakeReagentList(Fenoxitone, ref insertList);
		MakeReagentList(ColorRed, ref insertList);
		MakeReagentList(ColorGreen, ref insertList);
		MakeReagentList(ColorBlue, ref insertList);
		MakeReagentList(ColorYellow, ref insertList);
		MakeReagentList(ColorOrange, ref insertList);
		MakeReagentList(Pumpkin, ref insertList);
		MakeReagentList(Rice, ref insertList);
		MakeReagentList(Waspaloy, ref insertList);
		MakeReagentList(Stellite, ref insertList);
		MakeReagentList(Inconel, ref insertList);
		MakeReagentList(Hastelloy, ref insertList);
		MakeReagentList(Astroloy, ref insertList);
		MakeReagentList(Cobalt, ref insertList);
		MakeReagentList(Corn, ref insertList);
		MakeReagentList(Wheat, ref insertList);
		MakeReagentList(Biomass, ref insertList);
		MakeReagentList(Soy, ref insertList);
		MakeReagentList(Mushroom, ref insertList);
		MakeReagentList(Sugar, ref insertList);
		MakeReagentList(Cocoa, ref insertList);
		MakeReagentList(Cheese, ref insertList);
		return insertList;
	}

	public void Clear()
	{
		Flour.Quantity = 0.0;
		Milk.Quantity = 0.0;
		Egg.Quantity = 0.0;
		Iron.Quantity = 0.0;
		Gold.Quantity = 0.0;
		Carbon.Quantity = 0.0;
		Uranium.Quantity = 0.0;
		Copper.Quantity = 0.0;
		Steel.Quantity = 0.0;
		Hydrocarbon.Quantity = 0.0;
		Silver.Quantity = 0.0;
		Nickel.Quantity = 0.0;
		Lead.Quantity = 0.0;
		Electrum.Quantity = 0.0;
		Invar.Quantity = 0.0;
		Constantan.Quantity = 0.0;
		Solder.Quantity = 0.0;
		Plastic.Quantity = 0.0;
		Silicon.Quantity = 0.0;
		SalicylicAcid.Quantity = 0.0;
		Alcohol.Quantity = 0.0;
		Oil.Quantity = 0.0;
		Potato.Quantity = 0.0;
		Tomato.Quantity = 0.0;
		Fenoxitone.Quantity = 0.0;
		ColorRed.Quantity = 0.0;
		ColorGreen.Quantity = 0.0;
		ColorBlue.Quantity = 0.0;
		ColorYellow.Quantity = 0.0;
		ColorOrange.Quantity = 0.0;
		Pumpkin.Quantity = 0.0;
		Rice.Quantity = 0.0;
		Waspaloy.Quantity = 0.0;
		Stellite.Quantity = 0.0;
		Inconel.Quantity = 0.0;
		Hastelloy.Quantity = 0.0;
		Astroloy.Quantity = 0.0;
		Cobalt.Quantity = 0.0;
		Corn.Quantity = 0.0;
		Wheat.Quantity = 0.0;
		Biomass.Quantity = 0.0;
		Soy.Quantity = 0.0;
		Mushroom.Quantity = 0.0;
		Sugar.Quantity = 0.0;
		Cocoa.Quantity = 0.0;
		Cheese.Quantity = 0.0;
	}

	public bool Contains(Recipe recipe)
	{
		if (Flour.Quantity < recipe.Flour)
		{
			return false;
		}
		if (Milk.Quantity < recipe.Milk)
		{
			return false;
		}
		if (Egg.Quantity < recipe.Egg)
		{
			return false;
		}
		if (Iron.Quantity < recipe.Iron)
		{
			return false;
		}
		if (Gold.Quantity < recipe.Gold)
		{
			return false;
		}
		if (Carbon.Quantity < recipe.Carbon)
		{
			return false;
		}
		if (Uranium.Quantity < recipe.Uranium)
		{
			return false;
		}
		if (Copper.Quantity < recipe.Copper)
		{
			return false;
		}
		if (Steel.Quantity < recipe.Steel)
		{
			return false;
		}
		if (Hydrocarbon.Quantity < recipe.Hydrocarbon)
		{
			return false;
		}
		if (Silver.Quantity < recipe.Silver)
		{
			return false;
		}
		if (Nickel.Quantity < recipe.Nickel)
		{
			return false;
		}
		if (Lead.Quantity < recipe.Lead)
		{
			return false;
		}
		if (Electrum.Quantity < recipe.Electrum)
		{
			return false;
		}
		if (Invar.Quantity < recipe.Invar)
		{
			return false;
		}
		if (Constantan.Quantity < recipe.Constantan)
		{
			return false;
		}
		if (Solder.Quantity < recipe.Solder)
		{
			return false;
		}
		if (Plastic.Quantity < recipe.Plastic)
		{
			return false;
		}
		if (Silicon.Quantity < recipe.Silicon)
		{
			return false;
		}
		if (SalicylicAcid.Quantity < recipe.SalicylicAcid)
		{
			return false;
		}
		if (Alcohol.Quantity < recipe.Alcohol)
		{
			return false;
		}
		if (Oil.Quantity < recipe.Oil)
		{
			return false;
		}
		if (Potato.Quantity < recipe.Potato)
		{
			return false;
		}
		if (Tomato.Quantity < recipe.Tomato)
		{
			return false;
		}
		if (Fenoxitone.Quantity < recipe.Fenoxitone)
		{
			return false;
		}
		if (ColorRed.Quantity < recipe.ColorRed)
		{
			return false;
		}
		if (ColorGreen.Quantity < recipe.ColorGreen)
		{
			return false;
		}
		if (ColorBlue.Quantity < recipe.ColorBlue)
		{
			return false;
		}
		if (ColorYellow.Quantity < recipe.ColorYellow)
		{
			return false;
		}
		if (ColorOrange.Quantity < recipe.ColorOrange)
		{
			return false;
		}
		if (Pumpkin.Quantity < recipe.Pumpkin)
		{
			return false;
		}
		if (Rice.Quantity < recipe.Rice)
		{
			return false;
		}
		if (Waspaloy.Quantity < recipe.Waspaloy)
		{
			return false;
		}
		if (Stellite.Quantity < recipe.Stellite)
		{
			return false;
		}
		if (Inconel.Quantity < recipe.Inconel)
		{
			return false;
		}
		if (Hastelloy.Quantity < recipe.Hastelloy)
		{
			return false;
		}
		if (Astroloy.Quantity < recipe.Astroloy)
		{
			return false;
		}
		if (Cobalt.Quantity < recipe.Cobalt)
		{
			return false;
		}
		if (Corn.Quantity < recipe.Corn)
		{
			return false;
		}
		if (Wheat.Quantity < recipe.Wheat)
		{
			return false;
		}
		if (Biomass.Quantity < recipe.Biomass)
		{
			return false;
		}
		if (Soy.Quantity < recipe.Soy)
		{
			return false;
		}
		if (Mushroom.Quantity < recipe.Mushroom)
		{
			return false;
		}
		if (Sugar.Quantity < recipe.Sugar)
		{
			return false;
		}
		if (Cocoa.Quantity < recipe.Cocoa)
		{
			return false;
		}
		if (Cheese.Quantity < recipe.Cheese)
		{
			return false;
		}
		return true;
	}

	private static bool IsOnly(double recipeQuantity, Reagent reagent)
	{
		if (!(recipeQuantity > 0.0))
		{
			if (recipeQuantity == 0.0 && reagent.Quantity == 0.0)
			{
				goto IL_003e;
			}
		}
		else if (reagent.Quantity > 0.0)
		{
			goto IL_003e;
		}
		return false;
		IL_003e:
		return true;
	}

	public bool Equals(Recipe recipe)
	{
		if (!IsOnly(recipe.Flour, Flour))
		{
			return false;
		}
		if (!IsOnly(recipe.Milk, Milk))
		{
			return false;
		}
		if (!IsOnly(recipe.Egg, Egg))
		{
			return false;
		}
		if (!IsOnly(recipe.Iron, Iron))
		{
			return false;
		}
		if (!IsOnly(recipe.Gold, Gold))
		{
			return false;
		}
		if (!IsOnly(recipe.Carbon, Carbon))
		{
			return false;
		}
		if (!IsOnly(recipe.Uranium, Uranium))
		{
			return false;
		}
		if (!IsOnly(recipe.Copper, Copper))
		{
			return false;
		}
		if (!IsOnly(recipe.Steel, Steel))
		{
			return false;
		}
		if (!IsOnly(recipe.Hydrocarbon, Hydrocarbon))
		{
			return false;
		}
		if (!IsOnly(recipe.Silver, Silver))
		{
			return false;
		}
		if (!IsOnly(recipe.Nickel, Nickel))
		{
			return false;
		}
		if (!IsOnly(recipe.Lead, Lead))
		{
			return false;
		}
		if (!IsOnly(recipe.Electrum, Electrum))
		{
			return false;
		}
		if (!IsOnly(recipe.Invar, Invar))
		{
			return false;
		}
		if (!IsOnly(recipe.Constantan, Constantan))
		{
			return false;
		}
		if (!IsOnly(recipe.Solder, Solder))
		{
			return false;
		}
		if (!IsOnly(recipe.Plastic, Plastic))
		{
			return false;
		}
		if (!IsOnly(recipe.Silicon, Silicon))
		{
			return false;
		}
		if (!IsOnly(recipe.SalicylicAcid, SalicylicAcid))
		{
			return false;
		}
		if (!IsOnly(recipe.Alcohol, Alcohol))
		{
			return false;
		}
		if (!IsOnly(recipe.Oil, Oil))
		{
			return false;
		}
		if (!IsOnly(recipe.Potato, Potato))
		{
			return false;
		}
		if (!IsOnly(recipe.Tomato, Tomato))
		{
			return false;
		}
		if (!IsOnly(recipe.Fenoxitone, Fenoxitone))
		{
			return false;
		}
		if (!IsOnly(recipe.ColorRed, ColorRed))
		{
			return false;
		}
		if (!IsOnly(recipe.ColorGreen, ColorGreen))
		{
			return false;
		}
		if (!IsOnly(recipe.ColorBlue, ColorBlue))
		{
			return false;
		}
		if (!IsOnly(recipe.ColorYellow, ColorYellow))
		{
			return false;
		}
		if (!IsOnly(recipe.ColorOrange, ColorOrange))
		{
			return false;
		}
		if (!IsOnly(recipe.Pumpkin, Pumpkin))
		{
			return false;
		}
		if (!IsOnly(recipe.Rice, Rice))
		{
			return false;
		}
		if (!IsOnly(recipe.Waspaloy, Waspaloy))
		{
			return false;
		}
		if (!IsOnly(recipe.Stellite, Stellite))
		{
			return false;
		}
		if (!IsOnly(recipe.Inconel, Inconel))
		{
			return false;
		}
		if (!IsOnly(recipe.Hastelloy, Hastelloy))
		{
			return false;
		}
		if (!IsOnly(recipe.Astroloy, Astroloy))
		{
			return false;
		}
		if (!IsOnly(recipe.Cobalt, Cobalt))
		{
			return false;
		}
		if (!IsOnly(recipe.Corn, Corn))
		{
			return false;
		}
		if (!IsOnly(recipe.Wheat, Wheat))
		{
			return false;
		}
		if (!IsOnly(recipe.Biomass, Biomass))
		{
			return false;
		}
		if (!IsOnly(recipe.Soy, Soy))
		{
			return false;
		}
		if (!IsOnly(recipe.Mushroom, Mushroom))
		{
			return false;
		}
		if (!IsOnly(recipe.Sugar, Sugar))
		{
			return false;
		}
		if (!IsOnly(recipe.Cocoa, Cocoa))
		{
			return false;
		}
		if (!IsOnly(recipe.Cheese, Cheese))
		{
			return false;
		}
		return true;
	}

	public bool ContainsSome(Recipe recipe)
	{
		if (recipe.Flour > 0.0 && Flour.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Milk > 0.0 && Milk.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Egg > 0.0 && Egg.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Iron > 0.0 && Iron.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Gold > 0.0 && Gold.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Carbon > 0.0 && Carbon.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Uranium > 0.0 && Uranium.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Copper > 0.0 && Copper.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Steel > 0.0 && Steel.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Hydrocarbon > 0.0 && Hydrocarbon.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Silver > 0.0 && Silver.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Nickel > 0.0 && Nickel.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Lead > 0.0 && Lead.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Electrum > 0.0 && Electrum.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Invar > 0.0 && Invar.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Constantan > 0.0 && Constantan.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Solder > 0.0 && Solder.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Plastic > 0.0 && Plastic.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Silicon > 0.0 && Silicon.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.SalicylicAcid > 0.0 && SalicylicAcid.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Alcohol > 0.0 && Alcohol.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Oil > 0.0 && Oil.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Potato > 0.0 && Potato.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Tomato > 0.0 && Tomato.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Fenoxitone > 0.0 && Fenoxitone.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.ColorRed > 0.0 && ColorRed.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.ColorGreen > 0.0 && ColorGreen.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.ColorBlue > 0.0 && ColorBlue.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.ColorYellow > 0.0 && ColorYellow.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.ColorOrange > 0.0 && ColorOrange.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Pumpkin > 0.0 && Pumpkin.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Rice > 0.0 && Rice.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Waspaloy > 0.0 && Waspaloy.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Stellite > 0.0 && Stellite.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Inconel > 0.0 && Inconel.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Hastelloy > 0.0 && Hastelloy.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Astroloy > 0.0 && Astroloy.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Cobalt > 0.0 && Cobalt.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Corn > 0.0 && Corn.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Wheat > 0.0 && Wheat.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Biomass > 0.0 && Biomass.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Soy > 0.0 && Soy.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Mushroom > 0.0 && Mushroom.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Sugar > 0.0 && Sugar.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Cocoa > 0.0 && Cocoa.Quantity > 0.0)
		{
			return true;
		}
		if (recipe.Cheese > 0.0 && Cheese.Quantity > 0.0)
		{
			return true;
		}
		return false;
	}

	public void Read(RocketBinaryReader reader)
	{
		Flour.Quantity = reader.ReadSingle();
		Milk.Quantity = reader.ReadSingle();
		Egg.Quantity = reader.ReadSingle();
		Iron.Quantity = reader.ReadSingle();
		Gold.Quantity = reader.ReadSingle();
		Carbon.Quantity = reader.ReadSingle();
		Uranium.Quantity = reader.ReadSingle();
		Copper.Quantity = reader.ReadSingle();
		Steel.Quantity = reader.ReadSingle();
		Hydrocarbon.Quantity = reader.ReadSingle();
		Silver.Quantity = reader.ReadSingle();
		Nickel.Quantity = reader.ReadSingle();
		Lead.Quantity = reader.ReadSingle();
		Electrum.Quantity = reader.ReadSingle();
		Invar.Quantity = reader.ReadSingle();
		Constantan.Quantity = reader.ReadSingle();
		Solder.Quantity = reader.ReadSingle();
		Plastic.Quantity = reader.ReadSingle();
		Silicon.Quantity = reader.ReadSingle();
		SalicylicAcid.Quantity = reader.ReadSingle();
		Alcohol.Quantity = reader.ReadSingle();
		Oil.Quantity = reader.ReadSingle();
		Potato.Quantity = reader.ReadSingle();
		Tomato.Quantity = reader.ReadSingle();
		Fenoxitone.Quantity = reader.ReadSingle();
		ColorRed.Quantity = reader.ReadSingle();
		ColorGreen.Quantity = reader.ReadSingle();
		ColorBlue.Quantity = reader.ReadSingle();
		ColorYellow.Quantity = reader.ReadSingle();
		ColorOrange.Quantity = reader.ReadSingle();
		Pumpkin.Quantity = reader.ReadSingle();
		Rice.Quantity = reader.ReadSingle();
		Waspaloy.Quantity = reader.ReadSingle();
		Stellite.Quantity = reader.ReadSingle();
		Inconel.Quantity = reader.ReadSingle();
		Hastelloy.Quantity = reader.ReadSingle();
		Astroloy.Quantity = reader.ReadSingle();
		Cobalt.Quantity = reader.ReadSingle();
		Corn.Quantity = reader.ReadSingle();
		Wheat.Quantity = reader.ReadSingle();
		Biomass.Quantity = reader.ReadSingle();
		Soy.Quantity = reader.ReadSingle();
		Mushroom.Quantity = reader.ReadSingle();
		Sugar.Quantity = reader.ReadSingle();
		Cocoa.Quantity = reader.ReadSingle();
		Cheese.Quantity = reader.ReadSingle();
	}

	public void Write(RocketBinaryWriter writer)
	{
		writer.WriteSingle((float)Flour.Quantity);
		writer.WriteSingle((float)Milk.Quantity);
		writer.WriteSingle((float)Egg.Quantity);
		writer.WriteSingle((float)Iron.Quantity);
		writer.WriteSingle((float)Gold.Quantity);
		writer.WriteSingle((float)Carbon.Quantity);
		writer.WriteSingle((float)Uranium.Quantity);
		writer.WriteSingle((float)Copper.Quantity);
		writer.WriteSingle((float)Steel.Quantity);
		writer.WriteSingle((float)Hydrocarbon.Quantity);
		writer.WriteSingle((float)Silver.Quantity);
		writer.WriteSingle((float)Nickel.Quantity);
		writer.WriteSingle((float)Lead.Quantity);
		writer.WriteSingle((float)Electrum.Quantity);
		writer.WriteSingle((float)Invar.Quantity);
		writer.WriteSingle((float)Constantan.Quantity);
		writer.WriteSingle((float)Solder.Quantity);
		writer.WriteSingle((float)Plastic.Quantity);
		writer.WriteSingle((float)Silicon.Quantity);
		writer.WriteSingle((float)SalicylicAcid.Quantity);
		writer.WriteSingle((float)Alcohol.Quantity);
		writer.WriteSingle((float)Oil.Quantity);
		writer.WriteSingle((float)Potato.Quantity);
		writer.WriteSingle((float)Tomato.Quantity);
		writer.WriteSingle((float)Fenoxitone.Quantity);
		writer.WriteSingle((float)ColorRed.Quantity);
		writer.WriteSingle((float)ColorGreen.Quantity);
		writer.WriteSingle((float)ColorBlue.Quantity);
		writer.WriteSingle((float)ColorYellow.Quantity);
		writer.WriteSingle((float)ColorOrange.Quantity);
		writer.WriteSingle((float)Pumpkin.Quantity);
		writer.WriteSingle((float)Rice.Quantity);
		writer.WriteSingle((float)Waspaloy.Quantity);
		writer.WriteSingle((float)Stellite.Quantity);
		writer.WriteSingle((float)Inconel.Quantity);
		writer.WriteSingle((float)Hastelloy.Quantity);
		writer.WriteSingle((float)Astroloy.Quantity);
		writer.WriteSingle((float)Cobalt.Quantity);
		writer.WriteSingle((float)Corn.Quantity);
		writer.WriteSingle((float)Wheat.Quantity);
		writer.WriteSingle((float)Biomass.Quantity);
		writer.WriteSingle((float)Soy.Quantity);
		writer.WriteSingle((float)Mushroom.Quantity);
		writer.WriteSingle((float)Sugar.Quantity);
		writer.WriteSingle((float)Cocoa.Quantity);
		writer.WriteSingle((float)Cheese.Quantity);
	}

	public ReagentMixture()
	{
		Initialize();
	}

	public ReagentMixture GetRatioMixture()
	{
		return new ReagentMixture(this) / TotalReagents;
	}

	public ReagentMixture(Thing parent)
	{
		Initialize();
		Parent = parent;
	}

	public ReagentMixture(Reagent reagent, Thing parent)
	{
		Initialize();
		Parent = parent;
		Add(reagent);
	}

	public ReagentMixture(Reagent reagent)
	{
		Initialize();
		Add(reagent);
	}

	private double GetQuantity(Reagent reagent, int max)
	{
		return reagent.Quantity.Clamp(0.0, max);
	}

	public static ReagentMixture operator -(ReagentMixture left, ReagentMixture right)
	{
		left.Subtract(right);
		return left;
	}

	public static ReagentMixture operator +(ReagentMixture left, ReagentMixture right)
	{
		left.Add(right);
		return left;
	}

	public static ReagentMixture operator -(ReagentMixture left, Reagent right)
	{
		left.Subtract(right);
		return left;
	}

	public static ReagentMixture operator +(ReagentMixture left, Reagent right)
	{
		left.Add(right);
		return left;
	}

	private void MakePackagableReagentList(Reagent reagent, ref string masterString)
	{
		if (!(reagent.Quantity <= 0.0))
		{
			_reagentString = reagent.ToString();
			if (masterString != string.Empty)
			{
				masterString += "\n";
			}
			masterString += _reagentString;
		}
	}

	private void MakeReagentList(Reagent reagent, StringBuilder sb)
	{
		if (!(reagent.Quantity <= 0.0))
		{
			sb.AppendLine(reagent.ToString());
		}
	}

	private void MakeReagentList(Reagent reagent, StringBuilder sb, float scale)
	{
		if (!(reagent.Quantity <= 0.0))
		{
			sb.AppendLine(reagent.ToString(scale));
		}
	}

	private void MakeReagentList(Reagent reagent, ref List<StationFoundInInsert> insertList)
	{
		if (!(reagent.Quantity <= 0.0))
		{
			StationFoundInInsert item = reagent.ToStationpediaString();
			if (_reagentString != null)
			{
				insertList.Add(item);
			}
		}
	}

	public ReagentMixture(RocketBinaryReader reader)
	{
		Read(reader);
	}

	private void Serialize(Reagent reagent)
	{
		if (!(reagent.Quantity <= 0.0))
		{
			_saveData.Add(new ReagentSaveData(reagent));
		}
	}

	private void Serialize(List<ReagentMixIngredientSaveData> data, Reagent reagent)
	{
		if (!(reagent.Quantity <= 0.0))
		{
			data.Add(new ReagentMixIngredientSaveData(reagent));
		}
	}

	public double Get(int reagentHash)
	{
		return Get(Reagent.Find(reagentHash));
	}

	public bool Contains(Reagent reagentType)
	{
		return Get(reagentType) > 0.0;
	}

	public bool IsEmpty()
	{
		return !IsNotEmpty();
	}

	public bool IsNotEmpty()
	{
		if (Flour.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Milk.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Egg.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Iron.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Gold.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Carbon.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Uranium.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Copper.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Steel.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Hydrocarbon.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Silver.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Nickel.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Lead.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Electrum.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Invar.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Constantan.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Solder.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Plastic.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Silicon.Quantity > double.Epsilon)
		{
			return true;
		}
		if (SalicylicAcid.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Alcohol.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Oil.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Potato.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Tomato.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Fenoxitone.Quantity > double.Epsilon)
		{
			return true;
		}
		if (ColorRed.Quantity > double.Epsilon)
		{
			return true;
		}
		if (ColorGreen.Quantity > double.Epsilon)
		{
			return true;
		}
		if (ColorBlue.Quantity > double.Epsilon)
		{
			return true;
		}
		if (ColorYellow.Quantity > double.Epsilon)
		{
			return true;
		}
		if (ColorOrange.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Pumpkin.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Rice.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Waspaloy.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Stellite.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Inconel.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Hastelloy.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Astroloy.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Cobalt.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Corn.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Wheat.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Biomass.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Soy.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Mushroom.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Sugar.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Cocoa.Quantity > double.Epsilon)
		{
			return true;
		}
		if (Cheese.Quantity > double.Epsilon)
		{
			return true;
		}
		return false;
	}

	public ReagentMixture Normalize()
	{
		ReagentMixture reagentMixture = new ReagentMixture();
		foreach (Reagent allReagent in Reagent.AllReagents)
		{
			reagentMixture.Set(allReagent.ReagentId, Get(allReagent) / TotalReagents);
		}
		return reagentMixture;
	}
}
