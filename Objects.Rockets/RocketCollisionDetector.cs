using Assets.Scripts;
using Assets.Scripts.Objects;
using Objects.Rockets.Log;
using Objects.Rockets.Log.RocketEvents;
using UnityEngine;

namespace Objects.Rockets;

public class RocketCollisionDetector : MonoBehaviour
{
	private Rocket _rocket;

	private bool _isDestroyed;

	private const int COLLISION_STRUCTURE_DAMAGE = 1000;

	public void Initialize(Rocket rocket)
	{
		_rocket = rocket;
		_isDestroyed = false;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!GameManager.RunSimulation || _isDestroyed || other == null || other.isTrigger)
		{
			return;
		}
		Structure structure = Thing.Find(other) as Structure;
		if (structure == null || structure is LaunchMount)
		{
			return;
		}
		if (structure is StructureFuselage structureFuselage)
		{
			Rocket rocket = structureFuselage.RocketNetwork?.Rocket;
			if (rocket != null && rocket != _rocket)
			{
				rocket.ExplodeRocket(structureFuselage.ThingTransformPosition);
			}
		}
		else if (structure is IRocketInternals rocketInternals)
		{
			Rocket rocket2 = rocketInternals.RocketNetwork?.Rocket;
			if (rocket2 != null && rocket2 != _rocket)
			{
				rocket2.ExplodeRocket(rocketInternals.ThingTransformPosition);
			}
		}
		else
		{
			structure.DamageState.Damage(ChangeDamageType.Increment, 1000f, DamageUpdateType.Brute);
		}
		RocketLog.Append(new RocketCrashedEvent(_rocket, structure)).Forget();
		_rocket.ExplodeRocket(_rocket.RocketParentTransform.position);
		_isDestroyed = true;
	}
}
