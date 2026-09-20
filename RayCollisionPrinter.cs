using UnityEngine;

public class RayCollisionPrinter : Printer
{
	public CollisionCondition condition;

	public float conditionTime = 1f;

	public LayerMask layers;

	public Transform castPoint;

	public Vector3 positionOffset;

	public Vector3 rotationOffset;

	public float castLength = 1f;

	private CollisionData collision;

	private float timeElapsed;

	private bool delayPrinted;

	private void FixedUpdate()
	{
		CastCollision(Time.fixedDeltaTime);
	}

	private void CastCollision(float deltaTime)
	{
		Transform obj = ((castPoint != null) ? castPoint : base.transform);
		Quaternion quaternion = obj.rotation * Quaternion.Euler(rotationOffset);
		Vector3 origin = obj.position + quaternion * positionOffset;
		if (Physics.Raycast(new Ray(origin, quaternion * Vector3.forward), out var hitInfo, castLength, layers.value))
		{
			collision = new CollisionData(hitInfo.point, Quaternion.LookRotation(-hitInfo.normal, quaternion * Vector3.up), hitInfo.transform);
			if (condition == CollisionCondition.Constant)
			{
				PrintCollision(collision);
			}
			if (timeElapsed == 0f && condition == CollisionCondition.Enter)
			{
				PrintCollision(collision);
			}
			timeElapsed += deltaTime;
			if (condition == CollisionCondition.Delay && timeElapsed >= conditionTime && !delayPrinted)
			{
				PrintCollision(collision);
				delayPrinted = true;
			}
		}
		else
		{
			if (timeElapsed > 0f && (condition == CollisionCondition.Exit || (condition == CollisionCondition.Delay && timeElapsed < conditionTime)))
			{
				PrintCollision(collision);
			}
			timeElapsed = 0f;
			delayPrinted = false;
		}
	}

	private void PrintCollision(CollisionData collision)
	{
		Print(collision.position, collision.rotation, collision.surface);
	}

	private void OnDrawGizmos()
	{
		Transform obj = ((castPoint != null) ? castPoint : base.transform);
		Quaternion quaternion = obj.rotation * Quaternion.Euler(rotationOffset);
		Vector3 vector = obj.position + quaternion * positionOffset;
		Gizmos.color = Color.black;
		Gizmos.DrawRay(vector, quaternion * Vector3.up * 0.4f);
		Gizmos.color = Color.white;
		Gizmos.DrawRay(vector, quaternion * Vector3.forward * castLength);
	}
}
