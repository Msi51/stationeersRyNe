using System;
using UnityEngine;

namespace Assets.Scripts.Objects;

[Serializable]
public class RagdollTransformDefaults
{
	public Transform Transform;

	[ReadOnly]
	public Vector3 RestPosition;

	[ReadOnly]
	public Vector3 RestRotation;
}
