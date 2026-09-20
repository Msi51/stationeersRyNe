using Assets.Scripts.Objects.Entities;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class LadderEnd : Ladder
{
	protected override void OnTriggerEnter(Collider other)
	{
		if (!other.TryGetComponent<Human>(out var component))
		{
			return;
		}
		if ((bool)component.MovementController)
		{
			foreach (Collider movementCollider in component.MovementColliders)
			{
				Physics.IgnoreCollision(movementCollider, Collider, ignore: true);
			}
		}
		if (!component.TargetLadder)
		{
			component.OnPlayerLeaveLadder();
		}
	}

	protected override void OnTriggerStay(Collider other)
	{
		if (other.TryGetComponent<Human>(out var component) && !component.TargetLadder)
		{
			component.OnPlayerLeaveLadder();
		}
	}

	protected override void OnTriggerExit(Collider other)
	{
		if (!other.TryGetComponent<Human>(out var component))
		{
			return;
		}
		if ((bool)component.MovementController && !component.TargetLadder)
		{
			foreach (Collider movementCollider in component.MovementColliders)
			{
				Physics.IgnoreCollision(movementCollider, Collider, ignore: false);
			}
		}
		if (!component.TargetLadder)
		{
			component.OnPlayerLeaveLadder();
		}
	}
}
