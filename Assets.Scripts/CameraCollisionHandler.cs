using System.Collections;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts;

public class CameraCollisionHandler : MonoBehaviour
{
	public static float ColliderRadius;

	public static Transform CameraColliderTransform;

	public bool _colliding;

	private void Start()
	{
		CameraColliderTransform = base.transform;
		ColliderRadius = GetComponent<SphereCollider>().radius;
	}

	private bool IsCollideableObject(Collider collider)
	{
		if (CursorManager.Instance == null || collider != CursorManager.Instance.CursorTargetCollider)
		{
			return false;
		}
		if ((bool)(CursorManager.Instance.FoundThing as Structure))
		{
			return true;
		}
		return false;
	}

	private void OnTriggerStay(Collider other)
	{
		if (IsCollideableObject(other))
		{
			_colliding = true;
			float num = Vector3.Distance(CursorManager.CursorHit.point, CameraController.CameraOrigin);
			float num2 = Mathf.Clamp(ColliderRadius - num, 0.01f, 1f);
			num2 *= 1.1f;
			CameraController.CollisionOffset = Vector3.Lerp(CameraController.CollisionOffset, Vector3.back * num2, Time.deltaTime * 5f);
		}
	}

	private IEnumerator ResetPosition()
	{
		while (CameraController.CollisionOffset != Vector3.zero && !_colliding)
		{
			CameraController.CollisionOffset = Vector3.Lerp(CameraController.CollisionOffset, Vector3.zero, Time.deltaTime * 5f);
			yield return Yielders.EndOfFrame;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (IsCollideableObject(other))
		{
			StartCoroutine(ResetPosition());
			_colliding = false;
		}
	}
}
