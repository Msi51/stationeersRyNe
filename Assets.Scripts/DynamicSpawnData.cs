using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Reagents;
using UnityEngine;

namespace Assets.Scripts;

public class DynamicSpawnData : ThingSpawnData
{
	private const string SLOT_ID_ATTRIBUTE = "SlotId";

	[XmlAttribute("SlotId")]
	public string SlotId;

	private const string SLOT_INDEX_ATTRIBUTE = "SlotIndex";

	[XmlAttribute("SlotIndex")]
	public int SlotIndex = -1;

	private const string SLOT_CLASS_ATTRIBUTE = "SlotClass";

	[XmlAttribute("SlotClass")]
	public Slot.Class SlotClass;

	[XmlIgnore]
	public int SlotIdHash;

	public DynamicSpawnData()
	{
	}

	public override XElement Add(ref XElement parent, string elementName)
	{
		XElement xElement = base.Add(ref parent, elementName);
		if (!string.IsNullOrEmpty(SlotId))
		{
			XDocumentHelper.SetAttribute(xElement, "SlotId", SlotId);
		}
		if (SlotIndex >= 0)
		{
			XDocumentHelper.SetAttribute(xElement, "SlotIndex", SlotIndex.ToString());
		}
		if (SlotClass != Slot.Class.None)
		{
			XDocumentHelper.SetAttribute(xElement, "SlotClass", SlotClass.GetXmlEnumAttributeValueFromEnum());
		}
		return xElement;
	}

	public DynamicSpawnData(DynamicThing dynamicThing)
		: base(dynamicThing)
	{
		if (dynamicThing.ParentSlot == null)
		{
			SpawnPositionData = new SpawnPositionData
			{
				SpawnPositionRule = SpawnPositionRule.Explicit,
				Offset = new Vector3Reference(dynamicThing.Position),
				Rotation = new Vector3Reference(dynamicThing.Rotation.eulerAngles)
			};
		}
		if (dynamicThing is Item { CreatedReagentMixture: { TotalReagents: >0.0 } } item)
		{
			Actions.Add(new ReagentAction(item.CreatedReagentMixture));
		}
		if (dynamicThing.ParentSlot != null)
		{
			SlotIndex = dynamicThing.ParentSlot.SlotIndex;
		}
	}

	public override int GetChecksum()
	{
		return (int)(((uint)((((base.GetChecksum() ^ ((!string.IsNullOrEmpty(SlotId)) ? Animator.StringToHash(SlotId) : 0)) * 41) ^ SlotIndex) * 41) ^ (uint)SlotClass) * 41);
	}

	public override void Initialize(ModAbout mod)
	{
		base.Initialize(mod);
		SlotIdHash = ((!string.IsNullOrEmpty(SlotId)) ? Animator.StringToHash(SlotId) : 0);
	}

	public override Slot GetSlotIn(Thing parent)
	{
		if (SlotIndex >= 0)
		{
			return parent.GetSlot(SlotIndex);
		}
		if (SlotIdHash != 0)
		{
			return parent.GetNextFreeSlot(SlotId);
		}
		if (SlotClass != Slot.Class.None)
		{
			return parent.GetNextFreeSlot(SlotClass);
		}
		return parent.GetNextFreeSlot(base.Prefab);
	}
}
