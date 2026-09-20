using System;
using UnityEngine;

namespace Objects.RoboticArm;

[Serializable]
public struct NeedleRotator
{
	private float currentAngle;

	public Transform transform;

	public float lerpSpeed;

	public float minDegrees;

	public float maxDegrees;

	public void UpdateAngle(float percentage, float deltaTime)
	{
		currentAngle = Mathf.Lerp(currentAngle, Mathf.Lerp(minDegrees, maxDegrees, percentage), deltaTime * lerpSpeed);
	}

	public void ApplyRotationToTransform()
	{
		transform.localRotation = Quaternion.Euler(0f - currentAngle, 0f, 0f);
	}
}
