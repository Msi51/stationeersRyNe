using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Trading;

public class MovePlayerAction : ActionData
{
	private const string SLOT_ID_ATTRIBUTE = "SlotId";

	[XmlAttribute("SlotId")]
	public string SlotId;

	private const string SLOT_INDEX_ATTRIBUTE = "SlotIndex";

	[XmlAttribute("SlotIndex")]
	public int SlotIndex = -1;

	[XmlIgnore]
	public int SlotIdHash;

	private const string X_ELEMENT_NAME = "MovePlayer";

	public override string XElementName => "MovePlayer";

	public override void Initialize()
	{
		base.Initialize();
		if (!string.IsNullOrEmpty(SlotId))
		{
			SlotIdHash = Animator.StringToHash(SlotId);
		}
	}

	public override bool Execute<T>(T t, Entity player)
	{
		if (t is Thing parent)
		{
			MovePlayer(parent, player);
		}
		return true;
	}

	private void MovePlayer(Thing parent, Entity player)
	{
		int num = SlotIndex;
		if (num == -1)
		{
			for (int i = 0; i < parent.Slots.Count; i++)
			{
				if (parent.Slots[i].StringHash == SlotIdHash)
				{
					num = i;
					break;
				}
			}
		}
		if (num >= 0 && num < parent.TotalSlots)
		{
			OnServer.MoveToSlot(player, parent.Slots[num]);
		}
	}

	public override bool Execute(ref GasMixture tradable, int totalQuantitySold)
	{
		return false;
	}

	public override int GetChecksum()
	{
		return ((((!string.IsNullOrEmpty(SlotId)) ? Animator.StringToHash(SlotId) : 0) * 41) ^ SlotIndex) * 41;
	}

	public override void Add(ref XElement parentElement, string actionElementName)
	{
		if (!string.IsNullOrEmpty(SlotId) || SlotIndex != -1)
		{
			XElement element = XDocumentHelper.MakeElement(actionElementName, ref parentElement);
			if (!string.IsNullOrEmpty(SlotId))
			{
				XDocumentHelper.SetAttribute(element, "SlotId", SlotId);
			}
			if (SlotIndex >= 0)
			{
				XDocumentHelper.SetAttribute(element, "SlotIndex", SlotIndex.ToString());
			}
		}
	}

	public override void ToolTip(StringBuilder stringBuilder, int generations, Thing prefab = null)
	{
	}
}
