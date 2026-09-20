using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Reagents;

[Serializable]
public class Reagent : IEquatable<Reagent>
{
	public byte ReagentId;

	public static List<Reagent> AllReagentsSorted;

	private static Reagent[] _reagentLookup;

	private static Dictionary<int, Reagent> _reagentHashLookup;

	[NonSerialized]
	[XmlIgnore]
	public string TypeNameShort;

	[NonSerialized]
	[XmlIgnore]
	public ReagentMixture ParentMixture;

	[NonSerialized]
	[XmlIgnore]
	public string TypeName;

	[NonSerialized]
	[XmlIgnore]
	public string Unit;

	[XmlIgnore]
	[SerializeField]
	private double _quantity;

	[NonSerialized]
	[XmlIgnore]
	public float SpecificHeat = 45f;

	[NonSerialized]
	[XmlIgnore]
	public readonly int Hash;

	private const string BASIC_STRING = "<link=Reagent{2}><color=#B566FF>{0}</color></link> {1}";

	public static List<Reagent> AllReagents;

	public string DisplayName => Localization.GetName(this);

	public float HeatCapacity => (float)((double)SpecificHeat * _quantity);

	public double Quantity
	{
		get
		{
			return _quantity;
		}
		set
		{
			_quantity = Math.Max(value, 0.0);
			if (ParentMixture != null && ParentMixture.Parent != null)
			{
				if (NetworkManager.IsServer && GameManager.GameState == GameState.Running)
				{
					ParentMixture.Parent.FlagReagentStateChange(this);
				}
				ParentMixture.Parent.OnReagentUpdate();
			}
		}
	}

	public static implicit operator double(Reagent reagent)
	{
		return reagent.Quantity;
	}

	static Reagent()
	{
		AllReagents = new List<Reagent>
		{
			new Flour(0.0),
			new Milk(0.0),
			new Egg(0.0),
			new Iron(0.0),
			new Gold(0.0),
			new Carbon(0.0),
			new Uranium(0.0),
			new Copper(0.0),
			new Steel(0.0),
			new Hydrocarbon(0.0),
			new Silver(0.0),
			new Nickel(0.0),
			new Lead(0.0),
			new Electrum(0.0),
			new Invar(0.0),
			new Constantan(0.0),
			new Solder(0.0),
			new Plastic(0.0),
			new Silicon(0.0),
			new SalicylicAcid(0.0),
			new Alcohol(0.0),
			new Oil(0.0),
			new Potato(0.0),
			new Tomato(0.0),
			new Fenoxitone(0.0),
			new ColorRed(0.0),
			new ColorGreen(0.0),
			new ColorBlue(0.0),
			new ColorYellow(0.0),
			new ColorOrange(0.0),
			new Pumpkin(0.0),
			new Rice(0.0),
			new Waspaloy(0.0),
			new Stellite(0.0),
			new Inconel(0.0),
			new Hastelloy(0.0),
			new Astroloy(0.0),
			new Cobalt(0.0),
			new Corn(0.0),
			new Wheat(0.0),
			new Biomass(0.0),
			new Soy(0.0),
			new Mushroom(0.0),
			new Sugar(0.0),
			new Cocoa(0.0),
			new Cheese(0.0)
		};
		AllReagentsSorted = new List<Reagent>(AllReagents);
		AllReagentsSorted.Sort((Reagent a, Reagent b) => a.DisplayName.CompareTo(b.DisplayName));
	}

	public static void GenerateReagentTypeLookup()
	{
		_reagentLookup = new Reagent[AllReagents.Count];
		_reagentHashLookup = new Dictionary<int, Reagent>(AllReagents.Count);
		foreach (Reagent allReagent in AllReagents)
		{
			_reagentLookup[allReagent.ReagentId] = allReagent;
			_reagentHashLookup.Add(Animator.StringToHash(allReagent.TypeNameShort), allReagent);
		}
	}

	public static Reagent Find(int hash)
	{
		_reagentHashLookup.TryGetValue(hash, out var value);
		return value;
	}

	public static Reagent Find(string value)
	{
		return Find(Animator.StringToHash(value));
	}

	public override string ToString()
	{
		if (!(_quantity > 0.0))
		{
			return string.Empty;
		}
		return string.Format("<link=Reagent{2}><color=#B566FF>{0}</color></link> {1}", DisplayName, _quantity.ToStringPrefix(Unit, "yellow"), TypeNameShort);
	}

	public string ToString(float scale)
	{
		double num = _quantity * (double)scale;
		if (!(num > 0.0))
		{
			return string.Empty;
		}
		return string.Format("<link=Reagent{2}><color=#B566FF>{0}</color></link> {1}", DisplayName, num.ToStringPrefix(Unit, "yellow"), TypeNameShort);
	}

	public string PackagableToString()
	{
		string displayName = DisplayName;
		double quantity = Quantity;
		if (!(_quantity > 0.0))
		{
			return string.Empty;
		}
		return string.Format("<link=Reagent{2}><color=#B566FF>{0}</color></link> {1}", displayName, quantity.ToStringPrefix("", "yellow"), TypeNameShort);
	}

	public StationFoundInInsert ToStationpediaString()
	{
		StationFoundInInsert stationFoundInInsert = new StationFoundInInsert();
		if (_quantity > 0.0)
		{
			stationFoundInInsert.NameOfThing = DisplayName;
			stationFoundInInsert.QuantityOfThing = _quantity.ToStringPrefix(Unit, "yellow");
			return stationFoundInInsert;
		}
		return null;
	}

	public static Reagent Generate(string reagent)
	{
		return Generate(reagent, 0f);
	}

	public static Reagent Generate(string reagent, float quantity)
	{
		Type type = Type.GetType(reagent);
		if (type == null)
		{
			return null;
		}
		return (Reagent)Activator.CreateInstance(type, quantity);
	}

	public Reagent()
	{
		TypeName = GetType().ToString();
		TypeNameShort = TypeName.Replace("Reagents.", string.Empty);
		Hash = Animator.StringToHash(TypeNameShort);
	}

	public Reagent(double quantity)
	{
		Quantity = quantity;
		TypeName = GetType().ToString();
		TypeNameShort = TypeName.Replace("Reagents.", string.Empty);
		Hash = Animator.StringToHash(TypeNameShort);
	}

	public virtual void Burn()
	{
	}

	public override bool Equals(object obj)
	{
		return Hash == obj.GetHashCode();
	}

	public bool Equals(Reagent reagent)
	{
		if (reagent == null)
		{
			return false;
		}
		return Hash == reagent.Hash;
	}

	public override int GetHashCode()
	{
		return Hash;
	}

	public static Reagent Generate(byte reagentId, float quantity = 0f)
	{
		return reagentId switch
		{
			0 => new Flour(quantity), 
			1 => new Milk(quantity), 
			2 => new Egg(quantity), 
			3 => new Iron(quantity), 
			4 => new Gold(quantity), 
			5 => new Carbon(quantity), 
			6 => new Uranium(quantity), 
			7 => new Copper(quantity), 
			8 => new Steel(quantity), 
			9 => new Hydrocarbon(quantity), 
			10 => new Silver(quantity), 
			11 => new Nickel(quantity), 
			12 => new Lead(quantity), 
			13 => new Electrum(quantity), 
			14 => new Invar(quantity), 
			15 => new Constantan(quantity), 
			16 => new Solder(quantity), 
			17 => new Plastic(quantity), 
			18 => new Silicon(quantity), 
			19 => new SalicylicAcid(quantity), 
			20 => new Alcohol(quantity), 
			21 => new Oil(quantity), 
			22 => new Potato(quantity), 
			23 => new Tomato(quantity), 
			24 => new Fenoxitone(quantity), 
			25 => new ColorRed(quantity), 
			26 => new ColorGreen(quantity), 
			27 => new ColorBlue(quantity), 
			28 => new ColorYellow(quantity), 
			29 => new ColorOrange(quantity), 
			30 => new Pumpkin(quantity), 
			31 => new Rice(quantity), 
			32 => new Waspaloy(quantity), 
			33 => new Stellite(quantity), 
			34 => new Inconel(quantity), 
			35 => new Hastelloy(quantity), 
			36 => new Astroloy(quantity), 
			37 => new Cobalt(quantity), 
			38 => new Corn(quantity), 
			39 => new Wheat(quantity), 
			40 => new Biomass(quantity), 
			41 => new Soy(quantity), 
			42 => new Mushroom(quantity), 
			43 => new Sugar(quantity), 
			44 => new Cocoa(quantity), 
			45 => new Cheese(quantity), 
			_ => null, 
		};
	}
}
