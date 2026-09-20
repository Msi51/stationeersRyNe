namespace Objects.Rockets;

public readonly struct RocketCloneResult(RocketCloneStatus status, Rocket clone, int placed, int occupiedTargets)
{
	public readonly RocketCloneStatus Status = status;

	public readonly Rocket Clone = clone;

	public readonly int Placed = placed;

	public readonly int OccupiedTargets = occupiedTargets;

	public static RocketCloneResult Failed(RocketCloneStatus status, int occupiedTargets = 0)
	{
		return new RocketCloneResult(status, null, 0, occupiedTargets);
	}
}
