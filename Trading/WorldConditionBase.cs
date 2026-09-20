namespace Trading;

public abstract class WorldConditionBase : SerializedId, IChecksum
{
	public abstract bool Evaluate();

	public abstract int GetChecksum();
}
