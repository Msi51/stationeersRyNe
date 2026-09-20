using System;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

[Serializable]
public struct Gear
{
	public Transform GearTransform;

	public Vector3 RotateAxis;

	public float Ratio;

	public Vector3 localPosition;
}
