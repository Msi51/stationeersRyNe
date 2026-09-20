using UnityEngine;

public class ConstantDistance : MonoBehaviour
{
	public Transform Target;

	public float Distance = 10f;

	private Transform FlareEmitter;

	private Vector3 initialDirection;

	private Vector3 worldPosition;

	private Vector3 localDirection;

	private Vector3 localPosition;

	private Quaternion lastRotation;

	public void Start()
	{
		FlareEmitter = base.transform;
		initialDirection = (FlareEmitter.position - Target.position).normalized;
		CalculateLocals();
		CalculateWorldPosition();
		base.transform.position = worldPosition;
	}

	public void Update()
	{
		if (!lastRotation.Equals(Target.transform.rotation))
		{
			CalculateLocals();
		}
		CalculateWorldPosition();
		base.transform.position = worldPosition;
		lastRotation = Target.transform.rotation;
	}

	private void CalculateLocals()
	{
		localDirection = Target.InverseTransformDirection(initialDirection);
		localPosition = localDirection * Distance;
	}

	private void CalculateWorldPosition()
	{
		worldPosition = Target.TransformPoint(localPosition);
	}
}
