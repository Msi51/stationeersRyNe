using System;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

[Serializable]
public class Dial
{
	[Header("Dial")]
	[SerializeField]
	[Tooltip("Affects how fast the needle will move towards the current pressure")]
	private float lerpSpeed = 2f;

	[SerializeField]
	[Tooltip("Minimum degrees rotation on local Y")]
	private float needleMinimum;

	[SerializeField]
	[Tooltip("Maximum degrees rotation on local Y")]
	private float needleMaximum = 135f;

	[SerializeField]
	[Tooltip("The Highest value the dial can show")]
	private float maxValue = 100f;

	[SerializeField]
	[Tooltip("The needle (required)")]
	private GameObject needle;

	[SerializeField]
	private Collider dialCollider;

	private Transform _needleTransform;

	private float _lastAngle;

	[SerializeField]
	private Vector3 axisOfRotation = Vector3.up;

	public Collider Collider => dialCollider;

	public void Init()
	{
		_needleTransform = needle.transform;
	}

	public void UpdatePosition(float target, float deltaTime)
	{
		float t = Mathf.Clamp01(target / maxValue);
		float b = Mathf.Lerp(needleMinimum, needleMaximum, t);
		_lastAngle = Mathf.Lerp(_lastAngle, b, deltaTime * lerpSpeed);
		_needleTransform.localRotation = Quaternion.AngleAxis(_lastAngle, axisOfRotation);
	}
}
