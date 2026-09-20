using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Trading;
using UnityEngine;

namespace Objects.RoboticArm;

public interface ICollector : IReferencable, IEvaluable
{
	float RegistrationSquareDistance { get; }

	Vector3 CollectionPosition { get; }

	Room Room { get; }

	void RegisterItem(Item item);

	void DeRegisterItem(Item item);
}
