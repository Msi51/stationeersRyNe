namespace Assets.Scripts.Objects.Pipes;

public class ChuteJunction : Chute
{
	public Connection OutputConnection;

	public override SmallGrid GetOutputNeighbor(SmallGrid inputNeighbor)
	{
		return OutputConnection.GetChuteOrDevice();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		NextNeighbor = OutputConnection.GetChuteOrDevice();
	}

	public override void RebuildGridState()
	{
		base.RebuildGridState();
		OutputConnection?.SetGrids();
	}

	public override void SetDropPoint()
	{
		DropPosition = ((OutputConnection.GetChuteOrDevice() != null) ? base.ThingTransformPosition : OutputConnection.Transform.position);
		DropVelocity = (DropPosition - base.ThingTransformPosition) * Chute.VelocityScale;
	}

	public override void OnNeighborPlaced(SmallGrid neighbor)
	{
		base.OnNeighborPlaced(neighbor);
		NextNeighbor = OutputConnection.GetChuteOrDevice();
	}

	public override void OnNeighborRemoved(SmallGrid neighbor)
	{
		base.OnNeighborRemoved(neighbor);
		NextNeighbor = OutputConnection.GetChuteOrDevice();
	}
}
