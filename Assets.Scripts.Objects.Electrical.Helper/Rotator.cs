using UnityEngine;

namespace Assets.Scripts.Objects.Electrical.Helper;

public class Rotator : GameBase
{
	[Tooltip("Revolutions per Second")]
	[SerializeField]
	private float _maxSpeed;

	[Tooltip("Local Axis of rotation")]
	[SerializeField]
	private Vector3 _axis;

	[Tooltip("Revolutions per Second²")]
	[SerializeField]
	private float _spinUpAcceleration;

	private float _speed;

	public void DoUpdate(bool running)
	{
		float deltaTime = GameManager.DeltaTime;
		bool flag = false;
		if (running && _speed < _maxSpeed)
		{
			_speed = Mathf.Min(_speed + _spinUpAcceleration * deltaTime, _maxSpeed);
			flag = true;
		}
		else if (!running && _speed > 0f)
		{
			_speed = Mathf.Min(_speed - _spinUpAcceleration * deltaTime, _maxSpeed);
			flag = true;
		}
		else if (running)
		{
			flag = true;
		}
		if (flag)
		{
			Transform.Rotate(_axis, 360f * GameManager.DeltaTime * _speed);
		}
	}
}
