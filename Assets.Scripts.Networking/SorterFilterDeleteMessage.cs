using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class SorterFilterDeleteMessage : ProcessedMessage<SorterFilterDeleteMessage>
{
	public long SorterId;

	public int Index;

	public override void Process(long hostId)
	{
		Sorter sorter = Thing.Find<Sorter>(SorterId);
		if (sorter == null)
		{
			ConsoleWindow.PrintError($"SorterFilterDeleteMessage: sorter #{SorterId} not found");
			return;
		}
		if (Index < sorter.FilterReferences.Count)
		{
			sorter.RemoveFilter(Index);
		}
		foreach (SorterMotherboard controllingSorterMotherboard in sorter.ControllingSorterMotherboards)
		{
			controllingSorterMotherboard.RefreshSorterFilters(sorter);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		SorterId = reader.ReadInt64();
		Index = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(SorterId);
		writer.WriteInt32(Index);
	}
}
