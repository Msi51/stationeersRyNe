using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects;

public interface IVehicleCamera : IReferencable, IEvaluable
{
	Transform GetVehicleCameraTransform();
}
