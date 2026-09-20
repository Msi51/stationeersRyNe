using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Trading.Waypoints;
using UnityEngine;

namespace Objects.Electrical;

public class LandingPadTaxiThreshold : LandingPadModularDevice, IWaypoint
{
	[SerializeField]
	private WaypointType _waypointType;

	[SerializeField]
	private Transform _forwardEndTransform;

	public Connection ForwardEnd { get; private set; }

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
				ForwardEnd = openEnd;
				break;
			}
		}
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

	public override void OnStructureNetworkUpdated()
	{
		base.OnStructureNetworkUpdated();
		if (GameManager.RunSimulation)
		{
			AssessPower(null, false);
		}
	}

	protected override void AssessPower(CableNetwork cableNetwork, bool unused)
	{
		bool hasPower = base.LandingPadCenter != null && base.LandingPadCenter.OnOff;
		SetPower(cableNetwork, hasPower);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		InteractableType action = interactable.Action;
		if (action == InteractableType.OnOff || action == InteractableType.Powered)
		{
			SetCustomColor(OnOff && Powered);
			base.LandingPadNetwork?.LinkTaxiWaypoints();
		}
	}

	public void LinkTaxiWaypoints(LandingPadCenter landingPadCenter)
	{
		IWaypoint waypoint = this;
		Connection connection = ForwardEnd;
		VirtualWaypoint virtualWaypoint = new VirtualWaypoint(waypoint.WaypointPosition, waypoint.WaypointForward, WaypointType.RunwayStart);
		VirtualWaypoint virtualWaypoint2 = new VirtualWaypoint(waypoint.WaypointPosition, waypoint.WaypointForward, WaypointType.RunwayStop);
		LinkWaypoints(waypoint, virtualWaypoint2);
		waypoint = virtualWaypoint2;
		LinkWaypoints(waypoint, virtualWaypoint);
		waypoint = virtualWaypoint;
		while (!(waypoint as LandingPadCenter == landingPadCenter))
		{
			if (GetConnectedTaxiPiece(connection, out var nextTile, out var nextConnection))
			{
				LinkWaypoints(waypoint, nextTile);
				waypoint = nextTile;
				connection = nextConnection;
				continue;
			}
			LinkWaypoints(waypoint, landingPadCenter);
			break;
		}
	}

	private bool GetConnectedTaxiPiece(Connection connection, out LandingPadTaxiTile nextTile, out Connection nextConnection)
	{
		nextTile = null;
		nextConnection = null;
		Grid3 localGrid = base.GridController.WorldToLocalGrid(connection.Transform.position);
		SmallCell smallCell = base.GridController.GetSmallCell(localGrid);
		if (smallCell?.Other == null)
		{
			return false;
		}
		if (!(smallCell.Other is LandingPadTaxiTile landingPadTaxiTile))
		{
			return false;
		}
		if (landingPadTaxiTile == this)
		{
			return false;
		}
		if (!landingPadTaxiTile.TaxiTileIsConnected(connection, out var oppositeEnd))
		{
			return false;
		}
		nextTile = landingPadTaxiTile;
		nextConnection = oppositeEnd;
		return true;
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		base.LandingPadNetwork?.LinkTaxiWaypoints();
	}

	private void LinkWaypoints(IWaypoint from, IWaypoint to)
	{
		from.NextWaypoint = to;
		to.PreviousWaypoint = from;
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
