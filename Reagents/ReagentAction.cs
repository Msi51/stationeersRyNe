using System.Globalization;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Trading;

namespace Reagents;

[XmlType("SetReagents")]
public class ReagentAction : ActionData
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

	private const string X_ELEMENT_NAME = "Reagents";

	public override string XElementName => "Reagents";

	public ReagentAction()
	{
	}

	public ReagentAction(ReagentMixture thingReagentMixture)
	{
		Flour = thingReagentMixture.Flour;
		Milk = thingReagentMixture.Milk;
		Egg = thingReagentMixture.Egg;
		Iron = thingReagentMixture.Iron;
		Gold = thingReagentMixture.Gold;
		Carbon = thingReagentMixture.Carbon;
		Uranium = thingReagentMixture.Uranium;
		Copper = thingReagentMixture.Copper;
		Steel = thingReagentMixture.Steel;
		Hydrocarbon = thingReagentMixture.Hydrocarbon;
		Silver = thingReagentMixture.Silver;
		Nickel = thingReagentMixture.Nickel;
		Lead = thingReagentMixture.Lead;
		Electrum = thingReagentMixture.Electrum;
		Invar = thingReagentMixture.Invar;
		Constantan = thingReagentMixture.Constantan;
		Solder = thingReagentMixture.Solder;
		Plastic = thingReagentMixture.Plastic;
		Silicon = thingReagentMixture.Silicon;
		SalicylicAcid = thingReagentMixture.SalicylicAcid;
		Alcohol = thingReagentMixture.Alcohol;
		Oil = thingReagentMixture.Oil;
		Potato = thingReagentMixture.Potato;
		Tomato = thingReagentMixture.Tomato;
		Fenoxitone = thingReagentMixture.Fenoxitone;
		ColorRed = thingReagentMixture.ColorRed;
		ColorGreen = thingReagentMixture.ColorGreen;
		ColorBlue = thingReagentMixture.ColorBlue;
		ColorYellow = thingReagentMixture.ColorYellow;
		ColorOrange = thingReagentMixture.ColorOrange;
		Pumpkin = thingReagentMixture.Pumpkin;
		Rice = thingReagentMixture.Rice;
		Waspaloy = thingReagentMixture.Waspaloy;
		Stellite = thingReagentMixture.Stellite;
		Inconel = thingReagentMixture.Inconel;
		Hastelloy = thingReagentMixture.Hastelloy;
		Astroloy = thingReagentMixture.Astroloy;
		Cobalt = thingReagentMixture.Cobalt;
		Corn = thingReagentMixture.Corn;
		Wheat = thingReagentMixture.Wheat;
		Biomass = thingReagentMixture.Biomass;
		Soy = thingReagentMixture.Soy;
		Mushroom = thingReagentMixture.Mushroom;
		Sugar = thingReagentMixture.Sugar;
		Cocoa = thingReagentMixture.Cocoa;
	}

	public Recipe CreateRecipe()
	{
		return new Recipe(Flour, Milk, Egg, Iron, Gold, Carbon, Uranium, Copper, Steel, Hydrocarbon, Silver, Nickel, Lead, Electrum, Invar, Constantan, Solder, Plastic, Silicon, SalicylicAcid, Alcohol, Oil, Potato, Tomato, Fenoxitone, ColorRed, ColorGreen, ColorBlue, ColorYellow, ColorOrange, Pumpkin, Rice, Waspaloy, Stellite, Inconel, Hastelloy, Astroloy, Cobalt, Corn, Wheat, Biomass, Soy, Mushroom, Sugar, Cocoa);
	}

	public override bool Execute<T>(T t, Entity player)
	{
		if (!(t is Item item))
		{
			if (t is Thing { ReagentMixture: not null } thing)
			{
				thing.ReagentMixture = new ReagentMixture(CreateRecipe());
				return true;
			}
			return false;
		}
		item.TrySetCreatedReagentMixture(new ReagentMixture(CreateRecipe()));
		return true;
	}

	public override bool Execute(ref GasMixture tradable, int totalQuantitySold)
	{
		return false;
	}

	private void AppendLineForReagentToolTip(StringBuilder stringBuilder, int generations, string name, double value)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		if (value > 0.0)
		{
			stringBuilder.AppendLine(name + " " + value.ToStringPrefix("g", "yellow"));
		}
	}

	public override void ToolTip(StringBuilder stringBuilder, int generations, Thing prefab = null)
	{
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Flour), Flour);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Milk), Milk);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Egg), Egg);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Iron), Iron);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Gold), Gold);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Carbon), Carbon);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Uranium), Uranium);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Copper), Copper);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Steel), Steel);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Hydrocarbon), Hydrocarbon);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Silver), Silver);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Nickel), Nickel);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Lead), Lead);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Electrum), Electrum);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Invar), Invar);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Constantan), Constantan);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Solder), Solder);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Plastic), Plastic);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Silicon), Silicon);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.SalicylicAcid), SalicylicAcid);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Alcohol), Alcohol);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Oil), Oil);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Potato), Potato);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Tomato), Tomato);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Fenoxitone), Fenoxitone);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.ColorRed), ColorRed);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.ColorGreen), ColorGreen);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.ColorBlue), ColorBlue);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.ColorYellow), ColorYellow);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.ColorOrange), ColorOrange);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Pumpkin), Pumpkin);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Rice), Rice);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Waspaloy), Waspaloy);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Stellite), Stellite);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Inconel), Inconel);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Hastelloy), Hastelloy);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Astroloy), Astroloy);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Cobalt), Cobalt);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Corn), Corn);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Wheat), Wheat);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Biomass), Biomass);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Soy), Soy);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Mushroom), Mushroom);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Sugar), Sugar);
		AppendLineForReagentToolTip(stringBuilder, generations, Localization.GetName(ReagentMixture.Empty.Cocoa), Cocoa);
	}

	public override int GetChecksum()
	{
		return Flour.GetHashCode() ^ Milk.GetHashCode() ^ Egg.GetHashCode() ^ Iron.GetHashCode() ^ Gold.GetHashCode() ^ Carbon.GetHashCode() ^ Uranium.GetHashCode() ^ Copper.GetHashCode() ^ Steel.GetHashCode() ^ Hydrocarbon.GetHashCode() ^ Silver.GetHashCode() ^ Nickel.GetHashCode() ^ Lead.GetHashCode() ^ Electrum.GetHashCode() ^ Invar.GetHashCode() ^ Constantan.GetHashCode() ^ Solder.GetHashCode() ^ Plastic.GetHashCode() ^ Silicon.GetHashCode() ^ SalicylicAcid.GetHashCode() ^ Alcohol.GetHashCode() ^ Oil.GetHashCode() ^ Potato.GetHashCode() ^ Tomato.GetHashCode() ^ Fenoxitone.GetHashCode() ^ ColorRed.GetHashCode() ^ ColorGreen.GetHashCode() ^ ColorBlue.GetHashCode() ^ ColorYellow.GetHashCode() ^ ColorOrange.GetHashCode() ^ Pumpkin.GetHashCode() ^ Rice.GetHashCode() ^ Waspaloy.GetHashCode() ^ Stellite.GetHashCode() ^ Inconel.GetHashCode() ^ Hastelloy.GetHashCode() ^ Astroloy.GetHashCode() ^ Cobalt.GetHashCode() ^ Corn.GetHashCode() ^ Wheat.GetHashCode() ^ Biomass.GetHashCode() ^ Soy.GetHashCode() ^ Mushroom.GetHashCode() ^ Sugar.GetHashCode() ^ Cocoa.GetHashCode();
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		XElement element = XDocumentHelper.MakeElement(actionElementName, ref parentElement);
		if (Flour != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Flour", Flour.ToString(CultureInfo.InvariantCulture));
		}
		if (Milk != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Milk", Milk.ToString(CultureInfo.InvariantCulture));
		}
		if (Egg != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Egg", Egg.ToString(CultureInfo.InvariantCulture));
		}
		if (Iron != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Iron", Iron.ToString(CultureInfo.InvariantCulture));
		}
		if (Gold != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Gold", Gold.ToString(CultureInfo.InvariantCulture));
		}
		if (Carbon != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Carbon", Carbon.ToString(CultureInfo.InvariantCulture));
		}
		if (Uranium != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Uranium", Uranium.ToString(CultureInfo.InvariantCulture));
		}
		if (Copper != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Copper", Copper.ToString(CultureInfo.InvariantCulture));
		}
		if (Steel != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Steel", Steel.ToString(CultureInfo.InvariantCulture));
		}
		if (Hydrocarbon != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Hydrocarbon", Hydrocarbon.ToString(CultureInfo.InvariantCulture));
		}
		if (Silver != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Silver", Silver.ToString(CultureInfo.InvariantCulture));
		}
		if (Nickel != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Nickel", Nickel.ToString(CultureInfo.InvariantCulture));
		}
		if (Lead != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Lead", Lead.ToString(CultureInfo.InvariantCulture));
		}
		if (Electrum != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Electrum", Electrum.ToString(CultureInfo.InvariantCulture));
		}
		if (Invar != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Invar", Invar.ToString(CultureInfo.InvariantCulture));
		}
		if (Constantan != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Constantan", Constantan.ToString(CultureInfo.InvariantCulture));
		}
		if (Solder != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Solder", Solder.ToString(CultureInfo.InvariantCulture));
		}
		if (Plastic != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Plastic", Plastic.ToString(CultureInfo.InvariantCulture));
		}
		if (Silicon != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Silicon", Silicon.ToString(CultureInfo.InvariantCulture));
		}
		if (SalicylicAcid != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "SalicylicAcid", SalicylicAcid.ToString(CultureInfo.InvariantCulture));
		}
		if (Alcohol != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Alcohol", Alcohol.ToString(CultureInfo.InvariantCulture));
		}
		if (Oil != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Oil", Oil.ToString(CultureInfo.InvariantCulture));
		}
		if (Potato != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Potato", Potato.ToString(CultureInfo.InvariantCulture));
		}
		if (Tomato != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Tomato", Tomato.ToString(CultureInfo.InvariantCulture));
		}
		if (Fenoxitone != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Fenoxitone", Fenoxitone.ToString(CultureInfo.InvariantCulture));
		}
		if (ColorRed != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "ColorRed", ColorRed.ToString(CultureInfo.InvariantCulture));
		}
		if (ColorGreen != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "ColorGreen", ColorGreen.ToString(CultureInfo.InvariantCulture));
		}
		if (ColorBlue != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "ColorBlue", ColorBlue.ToString(CultureInfo.InvariantCulture));
		}
		if (ColorYellow != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "ColorYellow", ColorYellow.ToString(CultureInfo.InvariantCulture));
		}
		if (ColorOrange != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "ColorOrange", ColorOrange.ToString(CultureInfo.InvariantCulture));
		}
		if (Pumpkin != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Pumpkin", Pumpkin.ToString(CultureInfo.InvariantCulture));
		}
		if (Rice != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Rice", Rice.ToString(CultureInfo.InvariantCulture));
		}
		if (Waspaloy != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Waspaloy", Waspaloy.ToString(CultureInfo.InvariantCulture));
		}
		if (Stellite != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Stellite", Stellite.ToString(CultureInfo.InvariantCulture));
		}
		if (Inconel != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Inconel", Inconel.ToString(CultureInfo.InvariantCulture));
		}
		if (Hastelloy != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Hastelloy", Hastelloy.ToString(CultureInfo.InvariantCulture));
		}
		if (Astroloy != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Astroloy", Astroloy.ToString(CultureInfo.InvariantCulture));
		}
		if (Cobalt != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Cobalt", Cobalt.ToString(CultureInfo.InvariantCulture));
		}
		if (Corn != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Corn", Corn.ToString(CultureInfo.InvariantCulture));
		}
		if (Wheat != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Wheat", Wheat.ToString(CultureInfo.InvariantCulture));
		}
		if (Biomass != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Biomass", Biomass.ToString(CultureInfo.InvariantCulture));
		}
		if (Soy != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Soy", Soy.ToString(CultureInfo.InvariantCulture));
		}
		if (Mushroom != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Mushroom", Mushroom.ToString(CultureInfo.InvariantCulture));
		}
		if (Sugar != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Sugar", Sugar.ToString(CultureInfo.InvariantCulture));
		}
		if (Cocoa != 0.0)
		{
			XDocumentHelper.SetAttribute(element, "Cocoa", Cocoa.ToString(CultureInfo.InvariantCulture));
		}
	}
}
