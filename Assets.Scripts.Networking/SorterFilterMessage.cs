using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class SorterFilterMessage : ProcessedMessage<SorterFilterMessage>
{
	public long SorterId;

	public string PrefabName;

	public byte SlotType;

	public int Index;

	public override void Process(long hostId)
	{
		Sorter sorter = Thing.Find<Sorter>(SorterId);
		if (sorter == null)
		{
			ConsoleWindow.PrintError($"SorterFilterMessage: sorter #{SorterId} not found");
			return;
		}
		FilterReference obj = ((Index < sorter.FilterReferences.Count) ? sorter.FilterReferences[Index] : sorter.CreateFilter());
		obj.PrefabName = PrefabName;
		obj.SlotType = (Slot.Class)SlotType;
		foreach (SorterMotherboard controllingSorterMotherboard in sorter.ControllingSorterMotherboards)
		{
			controllingSorterMotherboard.RefreshSorterFilters(sorter);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		SorterId = reader.ReadInt64();
		PrefabName = reader.ReadString();
		SlotType = reader.ReadByte();
		Index = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(SorterId);
		writer.WriteString(PrefabName);
		writer.WriteByte(SlotType);
		writer.WriteInt32(Index);
	}
}
