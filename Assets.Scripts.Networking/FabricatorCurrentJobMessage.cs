using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI.Motherboard;

namespace Assets.Scripts.Networking;

public class FabricatorCurrentJobMessage : ProcessedMessage<FabricatorCurrentJobMessage>
{
	public long FabricatorId;

	public string PrefabName = string.Empty;

	public int Quantity = 1;

	public override void Process(long hostId)
	{
		Fabricator fabricator = Thing.Find<Fabricator>(FabricatorId);
		if (fabricator == null)
		{
			ConsoleWindow.PrintError($"FabricatorCurrentJobMessage: fabricator #{FabricatorId} not found");
			return;
		}
		if (PrefabName != string.Empty)
		{
			Fabricator fabricator2 = fabricator;
			if (fabricator2.CurrentJob == null)
			{
				fabricator2.CurrentJob = fabricator.CreateJob(addReference: false);
			}
			fabricator.CurrentJob.Prefab = ScreenConstructionJob.GetPrefab(PrefabName);
			fabricator.CurrentJob.Recipe = ScreenConstructionJob.GetRecipe(fabricator.CurrentJob.Prefab);
			fabricator.CurrentJob.Quantity = Quantity;
		}
		else
		{
			fabricator.CurrentJob = null;
		}
		foreach (ManufacturingMotherboard controllingManufacturingMotherboard in fabricator.ControllingManufacturingMotherboards)
		{
			controllingManufacturingMotherboard.RefreshCurrentJob(fabricator);
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		FabricatorId = reader.ReadInt64();
		PrefabName = reader.ReadString();
		Quantity = reader.ReadInt32();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(FabricatorId);
		writer.WriteString(PrefabName);
		writer.WriteInt32(Quantity);
	}
}
