using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class AtmosphericSeat : DeviceInternal, IExitable
{
	private Transform _cameraRig;

	public Transform CameraPoint;

	public virtual Vector3 ExitPosition => SeatSlot.Location.position + SeatSlot.Location.forward;

	public Slot SeatSlot => Slots[0];

	public bool FreeLook => true;

	public virtual void Exit(Human human)
	{
		human.MoveToWorld(GetExitPosition(human), human.ParentSlot.Parent.Rotation, Vector3.zero, Vector3.zero);
	}

	public Vector3 GetExitPosition(Entity entity)
	{
		return ExitPosition;
	}

	public Transform GetCameraPoint(Entity entity)
	{
		return CameraPoint;
	}
}
