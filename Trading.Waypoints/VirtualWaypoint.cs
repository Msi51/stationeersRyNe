using Objects.Electrical;
using UnityEngine;

namespace Trading.Waypoints;

public class VirtualWaypoint : IWaypoint
{
	private Vector3 _position;

	private Vector3 _forward;

	public Vector3 WaypointPosition => _position;

	public Vector3 WaypointForward => _forward;

	public Vector3 OriginalPosition { get; }

	public string WaypointName => "Virtual Waypoint";

	public WaypointType WaypointType { get; }

	public IWaypoint NextWaypoint { get; set; }

	public IWaypoint PreviousWaypoint { get; set; }

	public bool BeingDestroyed { get; set; }

	public void SetPosition(Vector3 position)
	{
		_position = position;
	}

	public VirtualWaypoint(Vector3 position, Vector3 forward, WaypointType waypointType)
	{
		OriginalPosition = (_position = position);
		_forward = forward;
		WaypointType = waypointType;
	}

	public virtual bool IsLinkedToCenter()
	{
		if (NextWaypoint is LandingPadCenter)
		{
			return true;
		}
		if (NextWaypoint == null)
		{
			return false;
		}
		return NextWaypoint.IsLinkedToCenter();
	}

	public void OnDeregister()
	{
	}
}
