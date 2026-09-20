namespace Trading;

public abstract class VarianceData : IChecksum
{
	public abstract float Apply(float value);

	public abstract int GetChecksum();
}
