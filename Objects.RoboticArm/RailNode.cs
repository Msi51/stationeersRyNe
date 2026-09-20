using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.RoboticArm;

public class RailNode : GameBase
{
	private Quaternion _armRotation;

	[SerializeField]
	private Vector3 _defaultRotation;

	[SerializeField]
	private Vector3 _flippedRotation;

	public int Index { get; set; }

	public IRoboticArmRail Rail { get; set; }

	public Quaternion ArmRotation => _armRotation;

	public Vector3 ArmPosition { get; private set; }

	public void Flip(bool flip)
	{
		Transform.localRotation = (flip ? Quaternion.Euler(_flippedRotation) : Quaternion.Euler(_defaultRotation));
		Init();
	}

	public void Init()
	{
		ArmPosition = Transform.position;
		_armRotation = Transform.rotation;
	}

	public void SetRotation(Quaternion rotation)
	{
		_armRotation = rotation;
	}

	public bool Equals(RailNode other)
	{
		if (!RocketMath.Approximately(ArmPosition, other.ArmPosition, 0.01f))
		{
			return false;
		}
		if (!RocketMath.Approximately(Mathf.Abs(Quaternion.Dot(ArmRotation, other.ArmRotation)), 1f, 0.01f))
		{
			return false;
		}
		return true;
	}
}
