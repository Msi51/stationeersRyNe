using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace Objects.Pipes;

public class ChuteOverflow : Chute
{
	[SerializeField]
	private GameObject _needle;

	private Connection _outputConnection;

	private Connection _overflowConnection;

	private bool _isOverflowing;

	private static readonly Vector3 _defaultRotation = new Vector3(0f, 0f, 0f);

	private static readonly Vector3 _overflowRotation = new Vector3(0f, 0f, 60f);

	private static readonly float _rotateTime = 0.4f;

	private Vector3 DropPoint(Connection connection)
	{
		return connection.Transform.position;
	}

	private new Vector3 DropVelocity(Connection connection)
	{
		return (connection.Transform.position - base.ThingTransformPosition) * Chute.VelocityScale;
	}

	public override void Awake()
	{
		base.Awake();
		SetConnections();
	}

	public override void RebuildGridState()
	{
		base.RebuildGridState();
		_outputConnection?.SetGrids();
		_overflowConnection?.SetGrids();
	}

	public override void OnServerTick(float deltaTime)
	{
		if (GameManager.RunSimulation && (bool)base.TransportSlot.Occupant && NextTickMove != OcclusionManager.LastOnServerTick && !CheckOutputConnection())
		{
			CheckOverflowConnection();
		}
	}

	private bool CheckOutputConnection()
	{
		SmallGrid chuteOrDevice = _outputConnection.GetChuteOrDevice();
		if (chuteOrDevice == null)
		{
			AnimateNeedle(overflow: false);
			Drop(_outputConnection);
			return true;
		}
		if (!(chuteOrDevice is IChute chute))
		{
			return false;
		}
		if (!Chute.IsValidInputConnection(chute.SmallGridOpenEnds, this))
		{
			return false;
		}
		if (chute.TransportSlot.Occupant != null)
		{
			return false;
		}
		AnimateNeedle(overflow: false);
		MoveToNeighbourChute(chute);
		return true;
	}

	private void CheckOverflowConnection()
	{
		SmallGrid chuteOrDevice = _overflowConnection.GetChuteOrDevice();
		if (chuteOrDevice == null)
		{
			AnimateNeedle(overflow: true);
			Drop(_overflowConnection);
		}
		else if (chuteOrDevice is IChute chute && Chute.IsValidInputConnection(chute.SmallGridOpenEnds, this) && !(chute.TransportSlot.Occupant != null))
		{
			AnimateNeedle(overflow: true);
			MoveToNeighbourChute(chute);
		}
	}

	private void Drop(Connection connection)
	{
		OnServer.MoveToWorld(base.TransportSlot.Occupant, DropPoint(connection), ThingTransform.rotation, DropVelocity(connection), Random.insideUnitSphere);
	}

	private void AnimateNeedle(bool overflow)
	{
		if (_isOverflowing != overflow)
		{
			_isOverflowing = overflow;
			LeanTween.cancel(_needle);
			LeanTween.rotateLocal(_needle, overflow ? _overflowRotation : _defaultRotation, _rotateTime);
		}
	}

	private void MoveToNeighbourChute(IChute chute)
	{
		NextNeighbor = chute as SmallGrid;
		chute.SetNeighbor(this);
		OnServer.MoveToSlot(base.TransportSlot.Occupant, chute.TransportSlot);
	}

	private void SetConnections()
	{
		foreach (Connection openEnd in OpenEnds)
		{
			switch (openEnd.ConnectionRole)
			{
			case ConnectionRole.Output:
				_outputConnection = openEnd;
				break;
			case ConnectionRole.Output2:
				_overflowConnection = openEnd;
				break;
			}
		}
	}
}
