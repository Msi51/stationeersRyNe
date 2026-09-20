using System.Collections.Generic;
using Assets.Scripts.Inventory;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class DynamicThingConstructor : Stackable, IConstructionKit
{
	public DynamicThing ConstructedPrefab;

	public float ConstructionTime = 0.5f;

	public int UsedQuantity = 1;

	public override int ConstructingSoundHash => Animator.StringToHash("ItemKitConstructing");

	public override int FinishedConstructingSoundHash => 0;

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletionRatio = 1f)
	{
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = ConstructionTime,
			ActionMessage = ActionStrings.Construct,
			ActionSoundHash = ConstructingSoundHash,
			ActionCompleteSoundHash = FinishedConstructingSoundHash
		};
		if (doAction && actionCompletionRatio >= 1f)
		{
			OnUseItem(UsedQuantity, RootParent);
		}
		return result;
	}

	public override bool OnUseItem(float quantity, Thing useOnThing)
	{
		Quaternion rotation;
		Vector3 vector;
		if (RootParent is Entity entity)
		{
			rotation = entity.EntityRotation;
			vector = entity.EntityForward;
		}
		else
		{
			rotation = RootParent.ThingTransform.rotation;
			vector = RootParent.ThingTransform.forward;
		}
		Vector3 vector2 = RootParent.ThingTransformPosition + vector + (Vector3.up - Vector3.up * ConstructedPrefab.Bounds.center.y);
		if (base.Quantity < 1)
		{
			return false;
		}
		DynamicThing thing = OnServer.CreateOld(ConstructedPrefab, vector2, rotation, RootParent.OwnerClientId);
		if (PaintableMaterial != null && CustomColor.Normal != null)
		{
			OnServer.SetCustomColor(thing, CustomColor.Index);
		}
		quantity = Mathf.Min(quantity, base.Quantity);
		return base.OnUseItem(quantity, useOnThing);
	}

	public List<Thing> GetConstructedPrefabs()
	{
		return new List<Thing> { ConstructedPrefab };
	}
}
