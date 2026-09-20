using System.Xml.Serialization;
using Assets.Scripts.Networking;

namespace Assets.Scripts.Objects.Electrical;

[XmlRoot]
public class FilterReference
{
	[XmlIgnore]
	public Sorter Sorter;

	[XmlElement]
	public string PrefabName = string.Empty;

	[XmlElement]
	public Slot.Class SlotType;

	public int Index => Sorter.FilterReferences.FindIndex((FilterReference f) => f == this);

	public bool IsTrue(DynamicThing dynamicThing)
	{
		if (dynamicThing.SlotType != SlotType || dynamicThing.SlotType == Slot.Class.None)
		{
			return dynamicThing.PrefabName == PrefabName;
		}
		return true;
	}

	public FilterReference()
	{
	}

	public FilterReference(Sorter sorter)
	{
		Sorter = sorter;
	}

	public FilterReference(string prefabName)
	{
		PrefabName = prefabName;
	}

	public FilterReference(Slot.Class slotType)
	{
		SlotType = slotType;
	}

	public FilterReference(Slot.Class slotType, Sorter sorter)
	{
		Sorter = sorter;
		SlotType = slotType;
	}

	public FilterReference(string prefabName, Sorter sorter)
	{
		Sorter = sorter;
		PrefabName = prefabName;
	}

	public void SendUpdate()
	{
		CreateNetworkMessage().Send();
	}

	public void SendDelete()
	{
		DeleteNetworkMessage().Send();
	}

	public SorterFilterMessage CreateNetworkMessage()
	{
		return new SorterFilterMessage
		{
			SorterId = Sorter.netId,
			Index = Index,
			PrefabName = PrefabName,
			SlotType = (byte)SlotType
		};
	}

	public SorterFilterDeleteMessage DeleteNetworkMessage()
	{
		return new SorterFilterDeleteMessage
		{
			SorterId = Sorter.netId,
			Index = Index
		};
	}
}
