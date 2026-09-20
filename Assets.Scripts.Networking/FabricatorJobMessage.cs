using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI.Motherboard;

namespace Assets.Scripts.Networking;

public class FabricatorJobMessage : ProcessedMessage<FabricatorJobMessage>
{
	public long FabricatorId;

	public string PrefabName;

	public int Index;

	public int Quantity;

	public override void Process(long hostId)
	{
		Fabricator fabricator = Thing.Find<Fabricator>(FabricatorId);
		if (fabricator == null)
		{
			ConsoleWindow.PrintError($"FabricatorJobMessage: fabricator #{FabricatorId} not found");
			return;
		}
		if (Index >= 0)
		{
			FabricatorJob obj = ((Index < fabricator.JobReferences.Count) ? fabricator.JobReferences[Index] : fabricator.CreateJob());
			obj.Prefab = ScreenConstructionJob.GetPrefab(PrefabName);
			obj.Recipe = ScreenConstructionJob.GetRecipe(obj.Prefab);
			obj.Quantity = Quantity;
		}
		else
		{
			FabricatorJob fabricatorJob = fabricator.CurrentJob ?? fabricator.CreateJob();
			fabricatorJob.Prefab = ScreenConstructionJob.GetPrefab(PrefabName);
			fabricatorJob.Recipe = ScreenConstructionJob.GetRecipe(fabricatorJob.Prefab);
			fabricatorJob.Quantity = Quantity;
			fabricator.CurrentJob = fabricatorJob;
		}
		foreach (ManufacturingMotherboard controllingManufacturingMotherboard in fabricator.ControllingManufacturingMotherboards)
		{
			controllingManufacturingMotherboard.RefreshJobs(fabricator);
			controllingManufacturingMotherboard.RefreshCurrentJob(fabricator);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		FabricatorId = reader.ReadInt64();
		PrefabName = reader.ReadString();
		Index = reader.ReadInt32();
		Quantity = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(FabricatorId);
		writer.WriteString(PrefabName);
		writer.WriteInt32(Index);
		writer.WriteInt32(Quantity);
	}
}
