using UnityEngine;

namespace Assets.Scripts.Objects;

public class SelectionInstance
{
	public bool TargetIsHuman;

	public bool IsWorldMode;

	public Vector3 Position;

	public Quaternion Rotation;

	public Bounds Bounds;

	public long ParentThingRefernceId = -1L;

	public int InteractableId = -1;

	public Vector3 GetScale()
	{
		if (TargetIsHuman)
		{
			return new Vector3(Bounds.size.x, Bounds.size.y, Bounds.size.z);
		}
		Vector3 vector = new Vector3(Bounds.size.x, Bounds.size.y, Bounds.size.z);
		if (IsWorldMode)
		{
			return Rotation * vector;
		}
		return vector;
	}

	public Vector3 GetPosition()
	{
		if (TargetIsHuman)
		{
			return Position;
		}
		if (IsWorldMode)
		{
			return Bounds.center;
		}
		return Position + Rotation * Bounds.center;
	}

	public Quaternion GetRotation()
	{
		return Rotation;
	}
}
