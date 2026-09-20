using System;
using Assets.Scripts;
using Assets.Scripts.Util;
using UnityEngine;

[Serializable]
public class ObjectAnimData
{
	[ReadOnly]
	public string name;

	public GameObject gameObject;

	public float tOffset;

	[ReadOnly]
	public Transform transform;

	public Vector3 position;

	public Vector3 rotation;

	public Vector3 scale;

	public bool IsValid()
	{
		if (gameObject != null)
		{
			return transform != null;
		}
		return false;
	}

	public void Apply()
	{
		transform.localPosition = position;
		transform.localRotation = Quaternion.Euler(rotation);
		transform.localScale = scale;
	}

	public void SetKeyframeValues(KeyFrameData newValues)
	{
		position = newValues.Position;
		rotation = newValues.Rotation;
		scale = newValues.Scale;
	}

	public bool MoveTowards(float speed)
	{
		Vector3 localPosition = transform.localPosition;
		Quaternion localRotation = transform.localRotation;
		Vector3 localScale = transform.localScale;
		Vector3 vector = Vector3.MoveTowards(localPosition, position, Time.deltaTime * speed);
		Quaternion quaternion = Quaternion.RotateTowards(localRotation, Quaternion.Euler(rotation), Time.deltaTime * speed * 360f);
		Vector3 vector2 = Vector3.MoveTowards(localScale, scale, Time.deltaTime * speed);
		if (RocketMath.Approximately(position, vector, 0.0005f) && RocketMath.Approximately(Quaternion.Euler(rotation), quaternion, 0.0005f) && RocketMath.Approximately(scale, vector2, 0.0005f))
		{
			Apply();
			return true;
		}
		transform.localPosition = vector;
		transform.localRotation = quaternion;
		transform.localScale = vector2;
		return false;
	}

	public void Lerp(ObjectAnimData state0, float t)
	{
		if ((bool)transform)
		{
			_ = tOffset;
			_ = state0.tOffset;
			t = Mathf.Clamp01(RocketMath.MapToScale(state0.tOffset, 1f + tOffset, 0f, 1f, t));
			transform.localPosition = Vector3.Lerp(state0.position, position, t);
			transform.localRotation = Quaternion.Lerp(Quaternion.Euler(state0.rotation), Quaternion.Euler(rotation), t);
			transform.localScale = Vector3.Lerp(state0.scale, scale, t);
		}
	}
}
