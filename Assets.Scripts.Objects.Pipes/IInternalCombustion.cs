using Assets.Scripts.Atmospherics;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public interface IInternalCombustion : IReferencable, IEvaluable
{
	new ushort NetworkUpdateFlags { get; set; }

	Atmosphere InternalAtmosphere { get; }

	Thing GetAsThing { get; }

	Transform ThrottleLever { get; }

	Transform CombustionLever { get; }
}
