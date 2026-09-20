public class ConcurrentRandom : RocketRandom
{
	public ConcurrentRandom(int seed)
		: base(seed)
	{
	}

	public override float Range(float min, float max)
	{
		lock (this)
		{
			return base.Range(min, max);
		}
	}

	public override int Range(int min, int max)
	{
		lock (this)
		{
			return base.Range(min, max);
		}
	}
}
