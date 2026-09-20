using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Assets.Scripts.Voxel;
using TerrainSystem;
using UnityEngine;

namespace Assets.Scripts;

public class CursorVoxel
{
	public static CursorVoxelMode Mode;

	public GameObject GameObject;

	public Transform Transform;

	public List<BoxCollider> Colliders = new List<BoxCollider>();

	public void UpdatePosition(Ray ray)
	{
		if (InventoryManager.ActiveHandSlot == null)
		{
			return;
		}
		Transform.position = ray.GetPoint(2f).GridCenter(1f);
		Mode = (InventoryManager.ActiveHandSlot.Contains<IMiningTool>(out var occupant) ? occupant.CursorVoxelMode : CursorVoxelMode.Default);
		foreach (BoxCollider collider in Colliders)
		{
			Vector3 worldPosition = Transform.position + collider.center;
			MinableType minableType = MinableType.None;
			if (VoxelTerrain.GetDensityWorldSpace(worldPosition) > 0f)
			{
				minableType = (MinableType)(((int?)Vein.GetVeinAtPosition(worldPosition)?.Type) ?? ((!(worldPosition.y <= 2f)) ? 1 : 255));
			}
			switch (Mode)
			{
			case CursorVoxelMode.Default:
				collider.enabled = minableType != MinableType.None;
				break;
			case CursorVoxelMode.Flatten:
				collider.enabled = minableType != MinableType.None && worldPosition.y > InventoryManager.Parent.ThingTransformPosition.y;
				break;
			}
		}
	}

	public CursorVoxel()
	{
		Vector3 zero = Vector3.zero;
		GameObject = new GameObject("~CursorVoxel")
		{
			layer = LayerMask.NameToLayer("CursorVoxel")
		};
		Transform = GameObject.transform;
		Vector3 vector = zero - Vector3.one;
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				for (int k = 0; k < 3; k++)
				{
					BoxCollider boxCollider = GameObject.AddComponent<BoxCollider>();
					boxCollider.enabled = false;
					boxCollider.center = vector + new Vector3(i, j, k);
					boxCollider.size = Vector3.one;
					boxCollider.isTrigger = true;
					Colliders.Add(boxCollider);
				}
			}
		}
	}
}
