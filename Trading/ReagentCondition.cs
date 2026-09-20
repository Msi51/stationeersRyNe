using System.Text;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using Assets.Scripts.UI.HelperHints;
using Assets.Scripts.Util;

namespace Trading;

public class ReagentCondition : ConditionComparable
{
	[XmlAttribute("Total")]
	public double Total;

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

	private void AppendLineForReagentToolTip(StringBuilder stringBuilder, int generations, string name, double value)
	{
		for (int i = 0; i < generations; i++)
		{
			stringBuilder.Append("    ");
		}
		if (value > 0.0)
		{
			stringBuilder.AppendLine(name + " " + StringManager.Get(value));
		}
	}

	public override int GetChecksum()
	{
		return base.GetChecksum() ^ Flour.GetHashCode() ^ Milk.GetHashCode() ^ Egg.GetHashCode() ^ Iron.GetHashCode() ^ Gold.GetHashCode() ^ Carbon.GetHashCode() ^ Uranium.GetHashCode() ^ Copper.GetHashCode() ^ Steel.GetHashCode() ^ Hydrocarbon.GetHashCode() ^ Silver.GetHashCode() ^ Nickel.GetHashCode() ^ Lead.GetHashCode() ^ Electrum.GetHashCode() ^ Invar.GetHashCode() ^ Constantan.GetHashCode() ^ Solder.GetHashCode() ^ Plastic.GetHashCode() ^ Silicon.GetHashCode() ^ SalicylicAcid.GetHashCode() ^ Alcohol.GetHashCode() ^ Oil.GetHashCode() ^ Potato.GetHashCode() ^ Tomato.GetHashCode() ^ Fenoxitone.GetHashCode() ^ ColorRed.GetHashCode() ^ ColorGreen.GetHashCode() ^ ColorBlue.GetHashCode() ^ ColorYellow.GetHashCode() ^ ColorOrange.GetHashCode() ^ Pumpkin.GetHashCode() ^ Rice.GetHashCode() ^ Waspaloy.GetHashCode() ^ Stellite.GetHashCode() ^ Inconel.GetHashCode() ^ Hastelloy.GetHashCode() ^ Astroloy.GetHashCode() ^ Cobalt.GetHashCode() ^ Corn.GetHashCode() ^ Wheat.GetHashCode() ^ Biomass.GetHashCode() ^ Soy.GetHashCode() ^ Mushroom.GetHashCode() ^ Sugar.GetHashCode() ^ Cocoa.GetHashCode();
	}

	public override bool Evaluate<T>(T t)
	{
		if (t is Thing { ReagentMixture: not null } thing)
		{
			if (Total > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.TotalReagents, (float)Total))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Flour > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Flour.Quantity, (float)Flour))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Milk > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Milk.Quantity, (float)Milk))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Egg > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Egg.Quantity, (float)Egg))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Iron > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Iron.Quantity, (float)Iron))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Gold > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Gold.Quantity, (float)Gold))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Carbon > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Carbon.Quantity, (float)Carbon))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Uranium > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Uranium.Quantity, (float)Uranium))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Copper > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Copper.Quantity, (float)Copper))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Steel > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Steel.Quantity, (float)Steel))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Hydrocarbon > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Hydrocarbon.Quantity, (float)Hydrocarbon))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Silver > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Silver.Quantity, (float)Silver))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Nickel > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Nickel.Quantity, (float)Nickel))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Lead > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Lead.Quantity, (float)Lead))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Electrum > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Electrum.Quantity, (float)Electrum))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Invar > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Invar.Quantity, (float)Invar))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Constantan > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Constantan.Quantity, (float)Constantan))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Solder > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Solder.Quantity, (float)Solder))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Plastic > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Plastic.Quantity, (float)Plastic))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Silicon > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Silicon.Quantity, (float)Silicon))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (SalicylicAcid > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.SalicylicAcid.Quantity, (float)SalicylicAcid))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Alcohol > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Alcohol.Quantity, (float)Alcohol))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Oil > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Oil.Quantity, (float)Oil))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Potato > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Potato.Quantity, (float)Potato))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Tomato > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Tomato.Quantity, (float)Tomato))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Fenoxitone > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Fenoxitone.Quantity, (float)Fenoxitone))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (ColorRed > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.ColorRed.Quantity, (float)ColorRed))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (ColorGreen > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.ColorGreen.Quantity, (float)ColorGreen))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (ColorBlue > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.ColorBlue.Quantity, (float)ColorBlue))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (ColorYellow > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.ColorYellow.Quantity, (float)ColorYellow))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (ColorOrange > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.ColorOrange.Quantity, (float)ColorOrange))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Pumpkin > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Pumpkin.Quantity, (float)Pumpkin))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Rice > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Rice.Quantity, (float)Rice))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Waspaloy > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Waspaloy.Quantity, (float)Waspaloy))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Stellite > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Stellite.Quantity, (float)Stellite))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Inconel > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Inconel.Quantity, (float)Inconel))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Hastelloy > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Hastelloy.Quantity, (float)Hastelloy))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Astroloy > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Astroloy.Quantity, (float)Astroloy))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Cobalt > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Cobalt.Quantity, (float)Cobalt))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Corn > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Corn.Quantity, (float)Corn))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Wheat > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Wheat.Quantity, (float)Wheat))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Biomass > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Biomass.Quantity, (float)Biomass))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Soy > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Soy.Quantity, (float)Soy))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Mushroom > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Mushroom.Quantity, (float)Mushroom))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Sugar > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Sugar.Quantity, (float)Sugar))
				{
					return base.Evaluate(t);
				}
				return false;
			}
			if (Cocoa > 0.0)
			{
				if (Compare((float)thing.ReagentMixture.Cocoa.Quantity, (float)Cocoa))
				{
					return base.Evaluate(t);
				}
				return false;
			}
		}
		return base.Evaluate(t);
	}

	protected override void AppendToolTip(StringBuilder stringBuilder, int generations)
	{
		AppendLineForReagentToolTip(stringBuilder, generations, "Flour", Flour);
		AppendLineForReagentToolTip(stringBuilder, generations, "Milk", Milk);
		AppendLineForReagentToolTip(stringBuilder, generations, "Egg", Egg);
		AppendLineForReagentToolTip(stringBuilder, generations, "Iron", Iron);
		AppendLineForReagentToolTip(stringBuilder, generations, "Gold", Gold);
		AppendLineForReagentToolTip(stringBuilder, generations, "Carbon", Carbon);
		AppendLineForReagentToolTip(stringBuilder, generations, "Uranium", Uranium);
		AppendLineForReagentToolTip(stringBuilder, generations, "Copper", Copper);
		AppendLineForReagentToolTip(stringBuilder, generations, "Steel", Steel);
		AppendLineForReagentToolTip(stringBuilder, generations, "Hydrocarbon", Hydrocarbon);
		AppendLineForReagentToolTip(stringBuilder, generations, "Silver", Silver);
		AppendLineForReagentToolTip(stringBuilder, generations, "Nickel", Nickel);
		AppendLineForReagentToolTip(stringBuilder, generations, "Lead", Lead);
		AppendLineForReagentToolTip(stringBuilder, generations, "Electrum", Electrum);
		AppendLineForReagentToolTip(stringBuilder, generations, "Invar", Invar);
		AppendLineForReagentToolTip(stringBuilder, generations, "Constantan", Constantan);
		AppendLineForReagentToolTip(stringBuilder, generations, "Solder", Solder);
		AppendLineForReagentToolTip(stringBuilder, generations, "Plastic", Plastic);
		AppendLineForReagentToolTip(stringBuilder, generations, "Silicon", Silicon);
		AppendLineForReagentToolTip(stringBuilder, generations, "SalicylicAcid", SalicylicAcid);
		AppendLineForReagentToolTip(stringBuilder, generations, "Alcohol", Alcohol);
		AppendLineForReagentToolTip(stringBuilder, generations, "Oil", Oil);
		AppendLineForReagentToolTip(stringBuilder, generations, "Potato", Potato);
		AppendLineForReagentToolTip(stringBuilder, generations, "Tomato", Tomato);
		AppendLineForReagentToolTip(stringBuilder, generations, "Fenoxitone", Fenoxitone);
		AppendLineForReagentToolTip(stringBuilder, generations, "ColorRed", ColorRed);
		AppendLineForReagentToolTip(stringBuilder, generations, "ColorGreen", ColorGreen);
		AppendLineForReagentToolTip(stringBuilder, generations, "ColorBlue", ColorBlue);
		AppendLineForReagentToolTip(stringBuilder, generations, "ColorYellow", ColorYellow);
		AppendLineForReagentToolTip(stringBuilder, generations, "ColorOrange", ColorOrange);
		AppendLineForReagentToolTip(stringBuilder, generations, "Pumpkin", Pumpkin);
		AppendLineForReagentToolTip(stringBuilder, generations, "Rice", Rice);
		AppendLineForReagentToolTip(stringBuilder, generations, "Waspaloy", Waspaloy);
		AppendLineForReagentToolTip(stringBuilder, generations, "Stellite", Stellite);
		AppendLineForReagentToolTip(stringBuilder, generations, "Inconel", Inconel);
		AppendLineForReagentToolTip(stringBuilder, generations, "Hastelloy", Hastelloy);
		AppendLineForReagentToolTip(stringBuilder, generations, "Astroloy", Astroloy);
		AppendLineForReagentToolTip(stringBuilder, generations, "Cobalt", Cobalt);
		AppendLineForReagentToolTip(stringBuilder, generations, "Corn", Corn);
		AppendLineForReagentToolTip(stringBuilder, generations, "Wheat", Wheat);
		AppendLineForReagentToolTip(stringBuilder, generations, "Biomass", Biomass);
		AppendLineForReagentToolTip(stringBuilder, generations, "Soy", Soy);
		AppendLineForReagentToolTip(stringBuilder, generations, "Mushroom", Mushroom);
	}

	public override void AppendHelperHint(HelperHintBuilder hintBuilder)
	{
		hintBuilder.Append(this);
	}
}
