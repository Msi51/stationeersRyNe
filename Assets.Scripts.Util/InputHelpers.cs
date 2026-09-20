using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Assets.Scripts.Util;

public static class InputHelpers
{
	public static GameObject HitGameObject;

	public static Vector3 HitNormal;

	private static Vector3 _dir;

	public static Vector3 GetIKAimPosition(LayerMask layer)
	{
		Vector3 zero = Vector3.zero;
		Vector3 mainCameraForward = CameraController.Instance.MainCameraForward;
		if (Physics.Raycast(CameraController.CameraOrigin, mainCameraForward, out var hitInfo, 20f, layer))
		{
			zero = hitInfo.point;
			HitNormal = hitInfo.normal;
			HitGameObject = hitInfo.transform.gameObject;
		}
		else
		{
			HitNormal = Vector3.zero;
			zero = CameraController.CameraOrigin + CameraController.CurrentCamera.transform.forward * 15f;
			HitGameObject = null;
		}
		return zero;
	}

	public static Vector3 GetCameraPhysicsPosition()
	{
		if ((bool)CameraController.Instance && (bool)CameraController.Instance.TrackedEntity)
		{
			_ = (bool)CameraController.Instance.CamRig;
			return CameraController.CameraOrigin;
		}
		return CameraController.CameraOrigin;
	}

	public static Ray GetCameraRay()
	{
		Vector3 origin = (((bool)CameraController.Instance && CameraController.Instance.ControlMode == CameraControlMode.MouseLook) ? CameraController.CameraOrigin : (CameraController.IsThirdPerson ? CameraController.CameraOrigin : CameraController.CameraOrigin));
		return new Ray(origin, CameraController.Instance.MainCameraForward);
	}

	public static Vector3 GetPlaneCameraCentrePosition()
	{
		Ray cameraRay = GetCameraRay();
		Vector3 zero = Vector3.zero;
		zero.y = CameraController.CameraOrigin.y - 1f;
		if (!new Plane(Vector3.up, zero).Raycast(cameraRay, out var enter))
		{
			return Vector3.zero;
		}
		if (float.IsInfinity(enter) || float.IsNaN(enter) || enter <= 0f)
		{
			return Vector3.zero;
		}
		return cameraRay.GetPoint(enter);
	}

	public static Vector3 GetCameraForwardGrid(float range, float offset, out Vector3 normal)
	{
		Ray cameraRay = GetCameraRay();
		float num = RocketGrid.GridLength * range + offset;
		if (Physics.Raycast(cameraRay, out var hitInfo, num, CursorManager.Instance.CursorHitMask))
		{
			normal = hitInfo.normal;
			return hitInfo.point;
		}
		normal = Vector3.zero;
		return cameraRay.GetPoint(num);
	}

	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360f)
		{
			angle += 360f;
		}
		if (angle > 360f)
		{
			angle -= 360f;
		}
		return Mathf.Clamp(angle, min, max);
	}

	public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
	{
		_dir = point - pivot;
		_dir = Quaternion.Euler(angles) * _dir;
		point = _dir + pivot;
		return point;
	}
}
