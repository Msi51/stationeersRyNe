using UnityEngine;

public class RayPositioner : Positioner
{
	public Transform rayTransform;

	public Vector3 positionOffset;

	public Vector3 rotationOffset;

	public float castLength = 100f;

	private void LateUpdate()
	{
		if (!WorldManager.IsGamePaused)
		{
			Transform obj = ((rayTransform != null) ? rayTransform : base.transform);
			Quaternion quaternion = obj.rotation * Quaternion.Euler(rotationOffset);
			Vector3 origin = obj.position + quaternion * positionOffset;
			Ray ray = new Ray(origin, quaternion * Vector3.forward);
			Reproject(ray, castLength, quaternion * Vector3.up);
		}
	}

	private void OnDrawGizmos()
	{
		Transform obj = ((rayTransform != null) ? rayTransform : base.transform);
		Quaternion quaternion = obj.rotation * Quaternion.Euler(rotationOffset);
		Vector3 vector = obj.position + quaternion * positionOffset;
		Gizmos.color = Color.black;
		Gizmos.DrawRay(vector, quaternion * Vector3.up * 0.4f);
		Gizmos.color = Color.white;
		Gizmos.DrawRay(vector, quaternion * Vector3.forward * castLength);
	}
}
