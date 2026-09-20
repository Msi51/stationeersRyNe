using System.Xml.Serialization;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts;

public class TradableItem : IChecksum
{
	[XmlAttribute("Id")]
	public string PrefabName;

	[XmlElement("Color")]
	public ColorSwatchReference colorSwatch;

	[XmlElement("Name")]
	public LocalizedStringReference Name;

	[XmlIgnore]
	public DynamicThing Prefab;

	[XmlAttribute("SlotId")]
	public string SlotId;

	[XmlAttribute("SlotIndex")]
	public int SlotIndex = -1;

	[XmlIgnore]
	public int SlotIdHash;

	public bool IsValid()
	{
		return Prefab != null;
	}

	public virtual void Initialize()
	{
		if (string.IsNullOrEmpty(PrefabName))
		{
			ConsoleWindow.PrintError("TradableItem does not have an Id.");
			return;
		}
		SlotIdHash = ((!string.IsNullOrEmpty(SlotId)) ? Animator.StringToHash(SlotId) : 0);
		Prefab = Assets.Scripts.Objects.Prefab.Find<DynamicThing>(PrefabName);
		if ((object)Prefab == null && WorldManager.Instance != null)
		{
			ConsoleWindow.PrintError("TradableItem " + PrefabName + " is not a valid prefab.");
		}
		colorSwatch?.Initialize();
	}

	public virtual int GetChecksum()
	{
		return (((!string.IsNullOrEmpty(PrefabName)) ? Animator.StringToHash(PrefabName) : 0) ^ (colorSwatch?.GetChecksum() ?? 0)) * 41;
	}
}
