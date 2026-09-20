using UnityEngine;

internal struct CollisionData(Vector3 Position, Quaternion Rotation, Transform Surface)
{
	public Vector3 position = Position;

	public Quaternion rotation = Rotation;

	public Transform surface = Surface;
}
