namespace Assets.Scripts.Util;

public readonly struct PoolSummary(string name, int activeCount)
{
	public readonly string Name = name;

	public readonly int ActiveCount = activeCount;
}
