using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Constructor : Stackable, IConstructionKit
{
	[Header("Constructor")]
	public Structure BuildStructure;

	public int QuantityUsed = 1;

	public override void OnUsePrimary(Vector3 targetLocation, Quaternion targetRotation, ulong steamId, bool authoringMode)
	{
		base.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode);
		Construct(targetLocation.ToGridPosition(), targetRotation, authoringMode, steamId);
	}

	public virtual void Construct(Grid3 localPosition, Quaternion targetRotation, bool authoringMode, ulong steamId)
	{
		if (authoringMode || OnUseItem(QuantityUsed, null))
		{
			CreateStructureInstance createStructureInstance = new CreateStructureInstance(BuildStructure, localPosition, targetRotation, steamId);
			if (PaintableMaterial != null && CustomColor.Normal != null)
			{
				createStructureInstance.CustomColor = CustomColor.Index;
			}
			SpawnConstruct(createStructureInstance);
		}
	}

	public static Structure SpawnConstruct(CreateStructureInstance instance)
	{
		if (GameManager.RunSimulation)
		{
			Structure structure = Thing.Create<Structure>(instance.Prefab, instance.WorldPosition, instance.WorldRotation, 0L);
			structure.SetStructureData(instance.LocalRotation, instance.OwnerClientId, instance.LocalGrid, instance.CustomColor);
			return structure;
		}
		if (NetworkManager.IsClient)
		{
			new ConstructionCreationMessage(instance).SendToServer();
		}
		return null;
	}

	public List<Thing> GetConstructedPrefabs()
	{
		return new List<Thing> { BuildStructure };
	}
}
