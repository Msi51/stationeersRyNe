using System.Collections.Generic;

namespace Trading.Waypoints;

public static class WaypointManager
{
	public static List<IWaypoint> AllWaypoints = new List<IWaypoint>();

	public static void Clear()
	{
		AllWaypoints.Clear();
	}

	private static void Initialise()
	{
		if (AllWaypoints == null)
		{
			AllWaypoints = new List<IWaypoint>();
		}
	}

	public static void Register(IWaypoint waypoint)
	{
		Initialise();
		if (!AllWaypoints.Contains(waypoint))
		{
			AllWaypoints.Add(waypoint);
		}
	}

	public static void Deregister(IWaypoint waypoint)
	{
		Initialise();
		waypoint.OnDeregister();
		AllWaypoints.Remove(waypoint);
	}

	public static IWaypoint CycleWaypoint(IWaypoint waypointToCycle, IWaypoint currentlyPointingTo)
	{
		if (AllWaypoints.Count < 2)
		{
			return null;
		}
		int num = AllWaypoints.IndexOf(currentlyPointingTo) + 1;
		for (int i = 0; i <= AllWaypoints.Count; i++)
		{
			if (num >= AllWaypoints.Count)
			{
				num = 0;
			}
			IWaypoint waypoint = AllWaypoints[num];
			if (waypoint != waypointToCycle)
			{
				WaypointType waypointType = waypoint.WaypointType;
				if (waypointType == WaypointType.Beacon || waypointType == WaypointType.PadCenter)
				{
					return waypoint;
				}
			}
			num++;
		}
		return null;
	}
}
