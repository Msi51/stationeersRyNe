using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Trading.Waypoints;
using UnityEngine;

namespace Objects.Electrical;

public class LandingPadTaxiTile : LandingPadTile, IWaypoint
{
	[SerializeField]
	private WaypointType _waypointType;

	[SerializeField]
	private Transform _forwardEndTransform;

	[SerializeField]
	private Transform _backEndTransform;

	private Connection _forwardEnd;

	private Connection _backEnd;

	public Vector3 WaypointPosition => base.Position - Vector3.up * 0.754f;

	public Vector3 WaypointForward => Transform.forward;

	public string WaypointName => DisplayName;

	public WaypointType WaypointType => _waypointType;

	public IWaypoint NextWaypoint { get; set; }

	public IWaypoint PreviousWaypoint { get; set; }

	public override void Awake()
	{
		base.Awake();
		foreach (Connection openEnd in OpenEnds)
		{
			if (openEnd.Transform == _forwardEndTransform)
			{
				_forwardEnd = openEnd;
			}
			else if (openEnd.Transform == _backEndTransform)
			{
				_backEnd = openEnd;
			}
		}
	}

	public override void FlashLights(bool flash)
	{
		_flash = flash;
		SetCustomColor(OnOff && _flash && IsLinkedToCenter());
	}

	public virtual bool IsLinkedToCenter()
	{
		if (NextWaypoint == base.LandingPadCenter)
		{
			return true;
		}
		if (NextWaypoint == null)
		{
			return false;
		}
		return NextWaypoint.IsLinkedToCenter();
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		WaypointManager.Register(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		WaypointManager.Deregister(this);
	}

	public bool TaxiTileIsConnected(Connection incomingConnection, out Connection oppositeEnd)
	{
		if (EndsAreConnected(_forwardEnd, incomingConnection))
		{
			oppositeEnd = _backEnd;
			return true;
		}
		if (EndsAreConnected(_backEnd, incomingConnection))
		{
			oppositeEnd = _forwardEnd;
			return true;
		}
		oppositeEnd = null;
		return false;
	}

	private bool EndsAreConnected(Connection end, Connection incomingConnection)
	{
		if ((end.ConnectionType & incomingConnection.ConnectionType) == 0)
		{
			return false;
		}
		Grid3 grid = base.GridController.WorldToLocalGrid(end.Transform.position, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		Grid3 grid2 = base.GridController.WorldToLocalGrid(incomingConnection.Transform.position, SmallGrid.SmallGridSize, SmallGrid.SmallGridOffset);
		return grid == grid2;
	}

	public void OnDeregister()
	{
		if (NextWaypoint != null)
		{
			NextWaypoint.PreviousWaypoint = null;
		}
		if (PreviousWaypoint != null)
		{
			PreviousWaypoint.NextWaypoint = null;
			if (PreviousWaypoint is Waypoint waypoint)
			{
				waypoint.TargetWaypoint = null;
			}
		}
	}
}
