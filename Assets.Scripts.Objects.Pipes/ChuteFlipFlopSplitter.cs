using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class ChuteFlipFlopSplitter : Chute
{
	[Header("Chute Splitter")]
	[SerializeField]
	private Transform _needle;

	private Connection[] _outputConnections;

	private readonly Vector3 _needleDown;

	private readonly Vector3 _needleUp = new Vector3(0f, 0f, 90f);

	private Connection CurrentOutput => _outputConnections[Mode];

	public override void Awake()
	{
		base.Awake();
		_outputConnections = OpenEnds.Where(delegate(Connection x)
		{
			ConnectionRole connectionRole = x.ConnectionRole;
			return connectionRole == ConnectionRole.Output || connectionRole == ConnectionRole.Output2;
		}).ToArray();
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		Vector3 endValue = ((Mode == 1) ? _needleDown : _needleUp);
		_needle.DOLocalRotate(endValue, 0.5f);
	}

	private void SetNextOutput()
	{
		Mode = (Mode + 1) % _outputConnections.Length;
		OnServer.Interact(base.InteractMode, Mode);
	}

	public override void RebuildGridState()
	{
		base.RebuildGridState();
		Connection[] outputConnections = _outputConnections;
		for (int i = 0; i < outputConnections.Length; i++)
		{
			outputConnections[i]?.SetGrids();
		}
	}

	protected override void OnPostItemMoved()
	{
		SetNextOutput();
		SetDropPoint();
	}

	public override SmallGrid GetOutputNeighbor(SmallGrid inputNeighbor)
	{
		return CurrentOutput.GetChuteOrDevice();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		NextNeighbor = CurrentOutput.GetChuteOrDevice();
	}

	public override void SetDropPoint()
	{
		DropPosition = (CurrentOutput.GetChuteOrDevice() ? base.ThingTransformPosition : CurrentOutput.Transform.position);
		DropVelocity = (DropPosition - base.ThingTransformPosition) * Chute.VelocityScale;
	}

	public override void OnNeighborPlaced(SmallGrid neighbor)
	{
		base.OnNeighborPlaced(neighbor);
		NextNeighbor = CurrentOutput.GetChuteOrDevice();
	}

	public override void OnNeighborRemoved(SmallGrid neighbor)
	{
		base.OnNeighborRemoved(neighbor);
		NextNeighbor = CurrentOutput.GetChuteOrDevice();
	}
}
