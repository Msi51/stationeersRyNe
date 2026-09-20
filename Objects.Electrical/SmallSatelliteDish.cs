using Assets.Scripts.Objects.Electrical;
using UnityEngine;

namespace Objects.Electrical;

public class SmallSatelliteDish : SatelliteDish
{
	[SerializeField]
	private Transform _horizontalPivot;

	public override float MovementSpeedHorizontal => 0.05f;

	public override float MovementSpeedVertical => 0.05f;

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
				DishForward = DishTransform.up;
				_isDirty = true;
				SetDishRotation();
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
				DishForward = DishTransform.up;
				_isDirty = true;
				SetDishRotation();
			}
		}
	}

	private void SetDishRotation()
	{
		float y = Mathf.Lerp(-90f, 270f, (float)_horizontal);
		float z = Mathf.Lerp(0f, 90f, (float)_vertical);
		_horizontalPivot.localRotation = Quaternion.Euler(0f, y, 0f);
		DishTransform.localRotation = Quaternion.Euler(0f, 0f, z);
	}
}
