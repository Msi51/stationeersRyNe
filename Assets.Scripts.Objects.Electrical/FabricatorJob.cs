using System.Xml.Serialization;
using Assets.Scripts.Networking;
using Assets.Scripts.UI.Motherboard;
using Reagents;

namespace Assets.Scripts.Objects.Electrical;

[XmlRoot]
public class FabricatorJob
{
	[XmlAttribute]
	public string PrefabName;

	[XmlAttribute]
	public int Quantity = 1;

	[XmlIgnore]
	public Fabricator Fabricator;

	[XmlIgnore]
	private DynamicThing _prefab;

	[XmlIgnore]
	public Recipe Recipe;

	[XmlIgnore]
	public DynamicThing Prefab
	{
		get
		{
			return _prefab;
		}
		set
		{
			_prefab = value;
			PrefabName = (_prefab ? _prefab.name : string.Empty);
		}
	}

	public int Index => Fabricator.JobReferences.FindIndex((FabricatorJob f) => f == this);

	public FabricatorJob()
	{
	}

	public FabricatorJob(Fabricator fabricator)
	{
		Fabricator = fabricator;
	}

	public FabricatorJob(string prefabName, Fabricator fabricator)
	{
		Fabricator = fabricator;
		Prefab = ScreenConstructionJob.GetPrefab(prefabName);
		Recipe = ScreenConstructionJob.GetRecipe(Prefab);
	}

	public FabricatorJob(FabricatorJob cloneJob, Fabricator fabricator)
	{
		Fabricator = fabricator;
		Prefab = ScreenConstructionJob.GetPrefab(cloneJob.PrefabName);
		Recipe = ScreenConstructionJob.GetRecipe(Prefab);
		Quantity = cloneJob.Quantity;
	}

	public void SendUpdate()
	{
		if (NetworkManager.IsClient)
		{
			CreateNetworkMessage().SendToServer();
		}
		else if (NetworkManager.IsServer)
		{
			CreateNetworkMessage().SendToClients();
		}
	}

	public void SendDelete()
	{
		if (NetworkManager.IsClient)
		{
			DeleteNetworkMessage().SendToServer();
		}
		else if (NetworkManager.IsServer)
		{
			DeleteNetworkMessage().SendToClients();
		}
	}

	public FabricatorJobMessage CreateNetworkMessage()
	{
		return new FabricatorJobMessage
		{
			FabricatorId = Fabricator.netId,
			Index = Index,
			Quantity = Quantity,
			PrefabName = (Prefab ? Prefab.name : string.Empty)
		};
	}

	public FabricatorCurrentJobMessage CurrentJobMessage()
	{
		return new FabricatorCurrentJobMessage
		{
			FabricatorId = Fabricator.netId,
			PrefabName = (Prefab ? Prefab.name : string.Empty),
			Quantity = Quantity
		};
	}

	public FabricatorJobDeleteMessage DeleteNetworkMessage()
	{
		return new FabricatorJobDeleteMessage
		{
			FabricatorId = Fabricator.netId,
			Index = Index
		};
	}
}
