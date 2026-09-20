using Assets.Scripts.Util;
using UnityEngine;

namespace TerrainSystem;

public class RotateManipulator : TransformGizmoManipulator
{
	private Vector3 _dragPositionOffset;

	private Quaternion _startRotation;

	public override void StartManipulate(Transform target, Ray cameraRay, Vector3 dragStartPositionWorldSpace)
	{
		Vector3 dragPositionOffset = RocketMath.LinePlaneIntersect(cameraRay.origin, cameraRay.direction, Axis, dragStartPositionWorldSpace);
		_dragPositionOffset = dragPositionOffset;
		_startRotation = target.rotation;
	}

	public override void Manipulate(Transform target, Ray cameraRay, Vector3 dragStartPositionWorldSpace)
	{
		Vector3 vector = RocketMath.LinePlaneIntersect(cameraRay.origin, cameraRay.direction, Axis, dragStartPositionWorldSpace);
		Vector3 normalized = (dragStartPositionWorldSpace - _dragPositionOffset).normalized;
		Vector3 normalized2 = (dragStartPositionWorldSpace - vector).normalized;
		float angle = Vector3.SignedAngle(normalized, normalized2, Axis);
		target.rotation = Quaternion.AngleAxis(angle, Axis) * _startRotation;
	}
}
