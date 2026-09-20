using System;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.FirstPerson;

[Serializable]
public class FirstPersonHelmet
{
	public bool Enabled = true;

	public GasMask HelmetSelf;

	public GameObject Helmet;

	public Transform Transform;

	public Vector3 OffsetPosition;

	public Vector3 OffsetRotation;

	public Vector3 Scale = Vector3.one;

	public Material[] FrostMaterials;

	public float FrostFadeSpeed = 0.01f;

	public float MaximumFieldOfViewScale = 1.7f;

	public float MinimumFieldOfViewScale = 0.8f;

	public float RotationSpeed = 42f;
}
