using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Networking;

public class CreateStructureInstance
{
	public Grid3 LocalGrid;

	public Quaternion LocalRotation;

	public Structure Prefab;

	public GridController GridController;

	public int CustomColor = -1;

	public ulong OwnerClientId;

	public Vector3 WorldPosition => GridController.LocalToWorld(LocalGrid);

	public Quaternion WorldRotation => LocalRotation;

	public CreateStructureInstance(Structure prefabToCreate, Structure oldPrefab)
	{
		Prefab = prefabToCreate;
		LocalGrid = oldPrefab.GridController.WorldToLocal(oldPrefab.ThingTransformPosition);
		LocalRotation = oldPrefab.ThingTransformLocalRotation;
		GridController = oldPrefab.GridController;
		OwnerClientId = oldPrefab.OwnerClientId;
		CustomColor = ((oldPrefab.CustomColor != null) ? oldPrefab.CustomColor.Index : 0);
	}

	public CreateStructureInstance(Structure prefabToCreate, Grid3 localGrid, Quaternion worldRotation, ulong ownerClientId, int colorIndex = -1)
	{
		Prefab = prefabToCreate;
		GridController = GridController.World;
		LocalGrid = localGrid;
		LocalRotation = worldRotation;
		OwnerClientId = ownerClientId;
		CustomColor = colorIndex;
	}
}
