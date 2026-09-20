using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;

namespace Assets.Scripts.Networking;

public class FabricatorJobDeleteMessage : ProcessedMessage<FabricatorJobDeleteMessage>
{
	public long FabricatorId;

	public int Index;

	public override void Process(long hostId)
	{
		Fabricator fabricator = Thing.Find<Fabricator>(FabricatorId);
		if (fabricator == null)
		{
			ConsoleWindow.PrintError($"FabricatorJobDeleteMessage: fabricator #{FabricatorId} not found");
			return;
		}
		if (Index < fabricator.JobReferences.Count)
		{
			if (Index >= 0)
			{
				fabricator.JobReferences.RemoveAt(Index);
			}
			else
			{
				fabricator.CurrentJob = null;
			}
		}
		foreach (ManufacturingMotherboard controllingManufacturingMotherboard in fabricator.ControllingManufacturingMotherboards)
		{
			controllingManufacturingMotherboard.RefreshCurrentJob(fabricator);
			controllingManufacturingMotherboard.RefreshJobs(fabricator);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		FabricatorId = reader.ReadInt64();
		Index = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(FabricatorId);
		writer.WriteInt32(Index);
	}
}
