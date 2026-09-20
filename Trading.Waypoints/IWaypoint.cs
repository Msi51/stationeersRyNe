using UnityEngine;

namespace Trading.Waypoints;

public interface IWaypoint
{
	Vector3 WaypointPosition { get; }

	Vector3 WaypointForward { get; }

	string WaypointName { get; }

	WaypointType WaypointType { get; }

	IWaypoint NextWaypoint { get; set; }

	IWaypoint PreviousWaypoint { get; set; }

	bool BeingDestroyed { get; set; }

	void OnDeregister();

	bool IsLinkedToCenter();
}
