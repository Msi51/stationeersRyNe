using System;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LargeSatelliteDish : SatelliteDish
{
	[SerializeField]
	private float dishRadius = 2.3f;

	[SerializeField]
	private Transform dummyRotatorH;

	[SerializeField]
	private Transform dummyRotatorV;

	[SerializeField]
	private Transform arm;

	[SerializeField]
	private Transform dish;

	[SerializeField]
	private Vector3 armStartingLocalPosition;

	[SerializeField]
	private Vector3 dishStartingLocalPosition;

	private static readonly float BaseMovementSpeed = 0.002f;

	private static readonly float centerOffset = 0.25f;

	public override float MovementSpeedHorizontal => BaseMovementSpeed / Mathf.Clamp01((float)Vertical + 0.25f);

	public override float MovementSpeedVertical => BaseMovementSpeed;

	public override float RotationTolerance => 1E-05f;

	public override double Vertical
	{
		get
		{
			return _vertical;
		}
		set
		{
			if (_vertical != value)
			{
				_vertical = value;
				dummyRotatorV.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -90f, (float)Vertical), 0f, 0f);
				DishForward = DishTransform.up;
				UpdateReceiverPosition();
				_isDirty = true;
			}
		}
	}

	public override double Horizontal
	{
		get
		{
			return _horizontal;
		}
		set
		{
			if (_horizontal != value)
			{
				_horizontal = value;
				dummyRotatorH.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, 360f, (float)Horizontal), 0f);
				DishForward = DishTransform.up;
				UpdateReceiverPosition();
				_isDirty = true;
			}
		}
	}

	private void UpdateReceiverPosition()
	{
		float num = Mathf.Lerp(-1f, 1f, (float)Horizontal);
		float num2 = RocketMath.MapToScale(0f, dishRadius, centerOffset, dishRadius, (float)((double)dishRadius * Vertical));
		float num3 = Mathf.Sin(num * MathF.PI + MathF.PI) * num2;
		float num4 = Mathf.Cos(num * MathF.PI + MathF.PI) * num2;
		arm.localPosition = new Vector3(num3 + armStartingLocalPosition.x, armStartingLocalPosition.y, armStartingLocalPosition.z);
		dish.localPosition = new Vector3(dishStartingLocalPosition.x, dishStartingLocalPosition.y, num4 + dishStartingLocalPosition.z);
	}
}
