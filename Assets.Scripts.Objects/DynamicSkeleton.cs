using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Localization2;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class DynamicSkeleton : DynamicThing
{
	[ReadOnly]
	public string CharacterName;

	[SerializeField]
	public List<RagdollPart> RagdollParts = new List<RagdollPart>();

	public PhysicMaterial Physics;

	private static readonly WaitForSeconds DestroyTimer = new WaitForSeconds(30f);

	private bool hasGravity;

	public override void Start()
	{
		base.Start();
		if (GameManager.RunSimulation && !base.Indestructable)
		{
			StartCoroutine(WaitThenDestroy());
		}
		SetRagdoll(active: true);
	}

	public override void PhysicsUpdate()
	{
		base.PhysicsUpdate();
		bool flag = WorldManager.HasGravityAtHeight(Transform.position.y);
		if (!hasGravity && (base.Room != null || flag))
		{
			foreach (RagdollPart ragdollPart in RagdollParts)
			{
				if (!(ragdollPart.Collider == null))
				{
					ragdollPart.Rigidbody.useGravity = true;
				}
			}
			hasGravity = true;
		}
		else
		{
			if (!hasGravity || base.Room != null || flag)
			{
				return;
			}
			foreach (RagdollPart ragdollPart2 in RagdollParts)
			{
				if (!(ragdollPart2.Collider == null))
				{
					ragdollPart2.Rigidbody.useGravity = false;
				}
			}
			hasGravity = false;
		}
	}

	public void ClearRagdoll()
	{
		foreach (RagdollPart ragdollPart in RagdollParts)
		{
			Object.DestroyImmediate(ragdollPart.Joint);
			Object.DestroyImmediate(ragdollPart.Rigidbody);
			Object.DestroyImmediate(ragdollPart.Collider);
		}
		RagdollParts.Clear();
	}

	public void SetRagdoll(bool active)
	{
		RigidBody.isKinematic = active;
		base.IsKinematic = active;
		foreach (RagdollPart ragdollPart in RagdollParts)
		{
			if (!(ragdollPart.Collider == null))
			{
				ragdollPart.Collider.enabled = active;
				bool flag = (ragdollPart.Rigidbody.useGravity = base.Room != null || WorldManager.HasGravityAtHeight(Transform.position.y));
				hasGravity = flag;
				ragdollPart.Rigidbody.isKinematic = !active;
			}
		}
	}

	public IEnumerator WaitThenDestroy()
	{
		yield return DestroyTimer;
		if (GameManager.RunSimulation)
		{
			HumanSkull humanSkull = OnServer.Create<HumanSkull>("HumanSkull", base.gameObject.transform.position, base.gameObject.transform.rotation);
			if (Random.Range(1, 1001) > 999)
			{
				humanSkull.CustomName = "Yorick";
			}
			if (!string.IsNullOrEmpty(CharacterName))
			{
				humanSkull.CustomName = GameStrings.CustomSkullName.AsString(CharacterName);
			}
		}
		OnServer.Destroy(this);
	}

	public void SetCustomName(string customName)
	{
		CharacterName = customName;
		CustomName = GameStrings.CustomSkeletonName.AsString(CharacterName);
	}
}
