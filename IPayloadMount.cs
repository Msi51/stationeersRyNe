using Assets.Scripts;
using Assets.Scripts.Objects;
using Trading;
using UnityEngine;

public interface IPayloadMount : IFastenedConnector, IReferencable, IEvaluable
{
	Slot PayloadSlot { get; }

	Vector3 Position { get; }
}
