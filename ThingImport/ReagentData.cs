using System.Xml.Serialization;
using Reagents;

namespace ThingImport;

public class ReagentData
{
	[XmlAttribute("Flour")]
	public double Flour;

	[XmlAttribute("Milk")]
	public double Milk;

	[XmlAttribute("Egg")]
	public double Egg;

	[XmlAttribute("Iron")]
	public double Iron;

	[XmlAttribute("Gold")]
	public double Gold;

	[XmlAttribute("Carbon")]
	public double Carbon;

	[XmlAttribute("Uranium")]
	public double Uranium;

	[XmlAttribute("Copper")]
	public double Copper;

	[XmlAttribute("Steel")]
	public double Steel;

	[XmlAttribute("Hydrocarbon")]
	public double Hydrocarbon;

	[XmlAttribute("Silver")]
	public double Silver;

	[XmlAttribute("Nickel")]
	public double Nickel;

	[XmlAttribute("Lead")]
	public double Lead;

	[XmlAttribute("Electrum")]
	public double Electrum;

	[XmlAttribute("Invar")]
	public double Invar;

	[XmlAttribute("Constantan")]
	public double Constantan;

	[XmlAttribute("Solder")]
	public double Solder;

	[XmlAttribute("Plastic")]
	public double Plastic;

	[XmlAttribute("Silicon")]
	public double Silicon;

	[XmlAttribute("SalicylicAcid")]
	public double SalicylicAcid;

	[XmlAttribute("Alcohol")]
	public double Alcohol;

	[XmlAttribute("Oil")]
	public double Oil;

	[XmlAttribute("Potato")]
	public double Potato;

	[XmlAttribute("Tomato")]
	public double Tomato;

	[XmlAttribute("Fenoxitone")]
	public double Fenoxitone;

	[XmlAttribute("ColorRed")]
	public double ColorRed;

	[XmlAttribute("ColorGreen")]
	public double ColorGreen;

	[XmlAttribute("ColorBlue")]
	public double ColorBlue;

	[XmlAttribute("ColorYellow")]
	public double ColorYellow;

	[XmlAttribute("ColorOrange")]
	public double ColorOrange;

	[XmlAttribute("Pumpkin")]
	public double Pumpkin;

	[XmlAttribute("Rice")]
	public double Rice;

	[XmlAttribute("Waspaloy")]
	public double Waspaloy;

	[XmlAttribute("Stellite")]
	public double Stellite;

	[XmlAttribute("Inconel")]
	public double Inconel;

	[XmlAttribute("Hastelloy")]
	public double Hastelloy;

	[XmlAttribute("Astroloy")]
	public double Astroloy;

	[XmlAttribute("Cobalt")]
	public double Cobalt;

	[XmlAttribute("Corn")]
	public double Corn;

	[XmlAttribute("Wheat")]
	public double Wheat;

	[XmlAttribute("Biomass")]
	public double Biomass;

	[XmlAttribute("Soy")]
	public double Soy;

	[XmlAttribute("Mushroom")]
	public double Mushroom;

	[XmlAttribute("Sugar")]
	public double Sugar;

	[XmlAttribute("Cocoa")]
	public double Cocoa;

	public ReagentMixture ToReagentMixture()
	{
		return new ReagentMixture
		{
			Flour = new Flour(Flour),
			Milk = new Milk(Milk),
			Egg = new Egg(Egg),
			Iron = new Iron(Iron),
			Gold = new Gold(Gold),
			Carbon = new Carbon(Carbon),
			Uranium = new Uranium(Uranium),
			Copper = new Copper(Copper),
			Steel = new Steel(Steel),
			Hydrocarbon = new Hydrocarbon(Hydrocarbon),
			Silver = new Silver(Silver),
			Nickel = new Nickel(Nickel),
			Lead = new Lead(Lead),
			Electrum = new Electrum(Electrum),
			Invar = new Invar(Invar),
			Constantan = new Constantan(Constantan),
			Solder = new Solder(Solder),
			Plastic = new Plastic(Plastic),
			Silicon = new Silicon(Silicon),
			SalicylicAcid = new SalicylicAcid(SalicylicAcid),
			Alcohol = new Alcohol(Alcohol),
			Oil = new Oil(Oil),
			Potato = new Potato(Potato),
			Tomato = new Tomato(Tomato),
			Fenoxitone = new Fenoxitone(Fenoxitone),
			ColorRed = new ColorRed(ColorRed),
			ColorGreen = new ColorGreen(ColorGreen),
			ColorBlue = new ColorBlue(ColorBlue),
			ColorYellow = new ColorYellow(ColorYellow),
			ColorOrange = new ColorOrange(ColorOrange),
			Pumpkin = new Pumpkin(Pumpkin),
			Rice = new Rice(Rice),
			Waspaloy = new Waspaloy(Waspaloy),
			Stellite = new Stellite(Stellite),
			Inconel = new Inconel(Inconel),
			Hastelloy = new Hastelloy(Hastelloy),
			Astroloy = new Astroloy(Astroloy),
			Cobalt = new Cobalt(Cobalt),
			Corn = new Corn(Corn),
			Wheat = new Wheat(Wheat),
			Biomass = new Biomass(Biomass),
			Soy = new Soy(Soy),
			Mushroom = new Mushroom(Mushroom),
			Sugar = new Sugar(Sugar),
			Cocoa = new Cocoa(Cocoa)
		};
	}
}
