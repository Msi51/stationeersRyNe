using Assets.Scripts.Util;
using UnityEngine;

namespace TerrainSystem;

public class TranslateManipulator : TransformGizmoManipulator
{
	private Vector3 _dragPositionOffset;

	public override void StartManipulate(Transform target, Ray cameraRay, Vector3 dragStartPositionWorldSpace)
	{
		if (RocketMath.ClosestPointsOnTwoLines(cameraRay.origin, cameraRay.direction, dragStartPositionWorldSpace, Axis, out var _, out var bClosest))
		{
			_dragPositionOffset = dragStartPositionWorldSpace - bClosest;
		}
	}

	public override void Manipulate(Transform target, Ray cameraRay, Vector3 dragStartPositionWorldSpace)
	{
		if (RocketMath.ClosestPointsOnTwoLines(cameraRay.origin, cameraRay.direction, dragStartPositionWorldSpace, Axis, out var _, out var bClosest))
		{
			target.position = bClosest + _dragPositionOffset;
		}
	}
}
