using Assets.Scripts.Util;
using UnityEngine;

namespace TerrainSystem;

public class Translate2DManipulator : TransformGizmoManipulator
{
	private Vector3 _dragPositionOffset;

	public override void StartManipulate(Transform target, Ray cameraRay, Vector3 dragStartPositionWorldSpace)
	{
		Vector3 vector = RocketMath.LinePlaneIntersect(cameraRay.origin, cameraRay.direction, Axis, dragStartPositionWorldSpace);
		_dragPositionOffset = dragStartPositionWorldSpace - vector;
	}

	public override void Manipulate(Transform target, Ray cameraRay, Vector3 dragStartPositionWorldSpace)
	{
		Vector3 vector = RocketMath.LinePlaneIntersect(cameraRay.origin, cameraRay.direction, Axis, dragStartPositionWorldSpace);
		target.position = vector + _dragPositionOffset;
	}
}
