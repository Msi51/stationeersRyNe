using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class Ladder : SmallGrid, ISmartRotatable
{
	public Collider Collider;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.SafetyCategory);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.SafetyCategory;
	}

	protected virtual void OnTriggerEnter(Collider other)
	{
		if (IsBroken || !other.TryGetComponent<Human>(out var component) || !component.MovementController)
		{
			return;
		}
		foreach (Collider movementCollider in component.MovementColliders)
		{
			Physics.IgnoreCollision(movementCollider, Collider, ignore: true);
		}
		component.OnPlayerStayLadder(this);
	}

	protected virtual void OnTriggerStay(Collider other)
	{
		if (IsBroken || !other.TryGetComponent<Human>(out var component) || !component.MovementController)
		{
			return;
		}
		foreach (Collider movementCollider in component.MovementColliders)
		{
			Physics.IgnoreCollision(movementCollider, Collider, ignore: true);
		}
		component.OnPlayerStayLadder(this);
	}

	protected virtual void OnTriggerExit(Collider other)
	{
		if (IsBroken || !other.TryGetComponent<Human>(out var component) || !component.MovementController)
		{
			return;
		}
		foreach (Collider movementCollider in component.MovementColliders)
		{
			Physics.IgnoreCollision(movementCollider, Collider, ignore: false);
		}
		component.OnPlayerLeaveLadder();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if ((bool)InventoryManager.ParentHuman && InventoryManager.ParentHuman.TargetLadder == this)
		{
			InventoryManager.ParentHuman.OnPlayerLeaveLadder();
		}
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}
}
