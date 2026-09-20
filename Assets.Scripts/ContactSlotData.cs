using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using CharacterCustomisation;
using ThingImport;
using Trading;
using UnityEngine;

namespace Assets.Scripts;

[XmlRoot("ContactSlot")]
public class ContactSlotData
{
	public class Select
	{
		[XmlElement("Conditions")]
		public List<ConditionCollection> ConditionCollections = new List<ConditionCollection>();

		public void Apply(TraderContact traderContact)
		{
			if (ConditionCollections.Count == 0)
			{
				return;
			}
			int num = 0;
			List<ConditionCollection> list = new List<ConditionCollection>();
			foreach (ConditionCollection conditionCollection in ConditionCollections)
			{
				if (conditionCollection.IsValid())
				{
					list.Add(conditionCollection);
					num += conditionCollection.Weight;
				}
			}
			int num2 = TraderContact.Randomize.Next(0, num);
			foreach (ConditionCollection item in list)
			{
				if (num2 < item.Weight)
				{
					item.Apply(traderContact);
					return;
				}
				num2 -= item.Weight;
			}
			throw new ArgumentOutOfRangeException();
		}
	}

	public class BulkMultiplier
	{
		[XmlAttribute("Value")]
		public float Value = 1f;
	}

	public abstract class SlotCondition
	{
		[XmlAttribute("Chance")]
		public float Chance = 1f;

		public abstract void Apply(TraderContact traderContact);

		public virtual bool IsValid()
		{
			return true;
		}
	}

	[XmlType("Plane")]
	public class PlaneCondition : SlotCondition
	{
		[XmlAttribute("Type")]
		public ShuttleType ShuttleType = ShuttleType.MediumPlane;

		public override void Apply(TraderContact traderContact)
		{
			if ((double)Chance > TraderContact.Randomize.NextDouble())
			{
				traderContact.SetShuttleType(ShuttleType);
			}
		}

		public override bool IsValid()
		{
			return PlanetaryAtmosphereSimulation.GlobalPressure >= PressurekPa.One;
		}
	}

	[XmlType("Shuttle")]
	public class ShuttleCondition : SlotCondition
	{
		[XmlAttribute("Type")]
		public ShuttleType ShuttleType = ShuttleType.Small;

		public override void Apply(TraderContact traderContact)
		{
			if ((double)Chance > TraderContact.Randomize.NextDouble())
			{
				traderContact.SetShuttleType(ShuttleType);
			}
		}
	}

	[XmlType("Environment")]
	public class EnvironmentCondition : SlotCondition
	{
		[XmlAttribute("Type")]
		public SpeciesClass SpeciesClassType = SpeciesClass.Human;

		public override void Apply(TraderContact traderContact)
		{
			if ((double)Chance > TraderContact.Randomize.NextDouble())
			{
				traderContact.RequiredPadEnvironment = SpeciesClassType;
			}
		}
	}

	public class ConditionCollection
	{
		[XmlAttribute("Weight")]
		public int Weight = 1000;

		[XmlElement("Plane", typeof(PlaneCondition))]
		[XmlElement("Shuttle", typeof(ShuttleCondition))]
		[XmlElement("Environment", typeof(EnvironmentCondition))]
		public List<SlotCondition> Conditions = new List<SlotCondition>();

		public void Apply(TraderContact contact)
		{
			foreach (SlotCondition condition in Conditions)
			{
				condition.Apply(contact);
			}
		}

		public bool IsValid()
		{
			foreach (SlotCondition condition in Conditions)
			{
				if (!condition.IsValid())
				{
					return false;
				}
			}
			return true;
		}
	}

	public class SlotIcon
	{
		[XmlAttribute("Path")]
		public string Path = string.Empty;

		[XmlIgnore]
		public Texture2D Icon;

		[XmlIgnore]
		public Sprite IconSprite;

		public void Initialise()
		{
			if (string.IsNullOrEmpty(Path))
			{
				ConsoleWindow.PrintError("Path not set for Trader Slot Icon");
				return;
			}
			Icon = StreamingAssetLoader.LoadTextureFromStreamingAssets(Path, TextureFormat.DXT5);
			if ((bool)Icon)
			{
				IconSprite = Sprite.Create(Icon, new Rect(0f, 0f, Icon.width, Icon.height), Vector2.one * 0.5f);
			}
		}
	}

	public static List<ContactSlotData> AllContactSlotData = new List<ContactSlotData>();

	[XmlAttribute("Id")]
	public string Id = string.Empty;

	[XmlIgnore]
	public int IdHash;

	[XmlElement("Icon")]
	public SlotIcon Icon;

	[XmlElement("MinimumWattsVisible")]
	public FloatRangeData MinimumWattsVisible;

	[XmlElement("WattsToResolve")]
	public FloatRangeData WattsToResolve;

	[XmlElement("MinimumWattsToContact")]
	public FloatRangeData MinimumWattsToContact;

	[XmlElement("SecondsToContact")]
	public FloatRangeData SecondsToContact;

	[XmlElement("LifeTime")]
	public FloatRangeData LifeTime;

	[XmlElement("DownTime")]
	public FloatRangeData DownTime;

	[XmlElement("Bulk")]
	public BulkMultiplier BulkData;

	[XmlElement("Select")]
	public Select ConditionSelect;

	[XmlElement("Trader")]
	public TraderSlotTraderSelectData TraderSelectData;

	[XmlElement("World")]
	public TraderSlotWorldData WorldCondition;

	public void Apply()
	{
	}

	public void Initialise()
	{
		AllContactSlotData.Add(this);
		Icon.Initialise();
		IdHash = Animator.StringToHash(Id);
	}

	public static ContactSlotData Get(int idHash)
	{
		foreach (ContactSlotData allContactSlotDatum in AllContactSlotData)
		{
			if (idHash == allContactSlotDatum.IdHash)
			{
				return allContactSlotDatum;
			}
		}
		return null;
	}
}
