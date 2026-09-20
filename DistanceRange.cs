public readonly struct DistanceRange
{
	public readonly double Minimum;

	public readonly double Maximum;

	public static DistanceRange zero;

	public DistanceRange(double min)
	{
		Minimum = min;
		Maximum = min;
	}

	public DistanceRange(double min, double max)
	{
		Minimum = min;
		Maximum = max;
	}

	public double Ratio(double distanceToPlayer)
	{
		return distanceToPlayer / Maximum;
	}

	static DistanceRange()
	{
		zero = new DistanceRange(0.0, 0.0);
	}
}
