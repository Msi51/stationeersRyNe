using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.UI;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Egg : Item, INutrition, IMicrowaveIngredient, IIngredient, ICentrifugable
{
	[Header("Egg")]
	public float BreakingForce = -1f;

	public StaticDecal SplatDecal;

	public Item DebrisEggTop;

	public Item DebrisEggBottom;

	private bool _hasBroken;

	public float NutritionValue;

	public float ProcessTime => 1f;

	public float MoodBonus => 0f;

	public float WaterMoles => 0f;

	public bool IsCentrifugeSmelt => false;

	public FoodQuality GetFoodQuality()
	{
		return FoodQuality.Raw;
	}

	public bool Equals(Recipe recipe)
	{
		return CreatedReagentMixture.Equals(recipe);
	}

	public float GetNutritionalValue()
	{
		return NutritionValue;
	}

	public override bool OnUseItem(float quantity, Thing useOnThing)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Destroy(this);
			return true;
		}
		return base.OnUseItem(quantity, useOnThing);
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.Edibles);
	}

	public override string GetStationpediaCategoryKey()
	{
		return StationpediaCategoryStrings.Edibles;
	}

	public override void OnCollisionEnter(Collision collision)
	{
		base.OnCollisionEnter(collision);
		Thing componentInParent = collision.transform.GetComponentInParent<Thing>();
		if ((bool)componentInParent && componentInParent.HasAuthority)
		{
			Vector3 contactPoint = collision.contacts[0].point + collision.contacts[0].normal * 0.2f;
			if (GameManager.RunSimulation)
			{
				OnNetworkCollision(contactPoint, collision.relativeVelocity, componentInParent, collision.impulse.magnitude);
				return;
			}
			NetworkMessages.CollisionMessage collisionMessage = new NetworkMessages.CollisionMessage();
			collisionMessage.ContactPoint = contactPoint;
			collisionMessage.RelativeVelocity = collision.relativeVelocity;
			collisionMessage.ThingId = base.netId;
			collisionMessage.OtherThingId = componentInParent.netId;
			collisionMessage.ImpulseMagnitude = collision.impulse.magnitude;
			collisionMessage.SendToServer();
		}
	}

	public override void OnNetworkCollision(Vector3 contactPoint, Vector3 relativeVelocity, Thing otherThing, float impulseMagnitude)
	{
		base.OnNetworkCollision(contactPoint, relativeVelocity, otherThing, impulseMagnitude);
		if (ShouldBreakFromCollision(relativeVelocity.magnitude) && HitIsStructure(contactPoint, relativeVelocity))
		{
			SpawnBrokenEggshells();
			_hasBroken = true;
			OnServer.Destroy(this);
		}
	}

	private bool ShouldBreakFromCollision(float speed)
	{
		if (!_hasBroken && BreakingForce >= 0f)
		{
			return speed > BreakingForce;
		}
		return false;
	}

	private bool HitIsStructure(Vector3 contactPoint, Vector3 relativeVelocity)
	{
		Vector3 vector = -relativeVelocity;
		vector = (vector + (RigidBody.useGravity ? Physics.gravity : Vector3.zero)).normalized;
		RaycastHit[] array = Physics.RaycastAll(contactPoint, vector, 2f);
		for (int i = 0; i < array.Length; i++)
		{
			if ((bool)array[i].transform.GetComponentInParent<Structure>())
			{
				return true;
			}
		}
		return false;
	}

	private void SpawnBrokenEggshells()
	{
		Vector3 vector = ThingTransform.position;
		Quaternion rotation = ThingTransform.rotation;
		OnServer.Create<Item>(DebrisEggTop, vector + ThingTransform.up * 0.1f, rotation);
		OnServer.Create<Item>(DebrisEggBottom, vector - ThingTransform.up * 0.1f, rotation);
		EffectManager.CreateEggSplatEffect(vector);
	}

	public float Nutrition(float useAmount)
	{
		return NutritionValue;
	}

	public float EatAmount(Entity eater)
	{
		return 1f;
	}

	public float EatTime(float quantityToEat)
	{
		return 1f * quantityToEat;
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		if (RootParent == this || NutritionValue <= 0f)
		{
			return base.OnUseSecondary(doAction);
		}
		if (!(RootParent is Human human))
		{
			return base.OnUseSecondary(doAction);
		}
		float num = EatAmount(human);
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = EatTime(num),
			ActionMessage = ActionStrings.Consume,
			ActionSoundHash = Item.EatingHash,
			ActionCompleteSoundHash = Item.EatingFinishedHash
		};
		if (!doAction)
		{
			return result;
		}
		OnUseItem(num * actionCompletedRatio, RootParent);
		human.OnFoodEaten(this);
		return result;
	}

	public ReagentMixture CentrifugeProcessUnit()
	{
		return GetTotalReagentMixture();
	}
}
