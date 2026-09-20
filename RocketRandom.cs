using System;

public class RocketRandom
{
	public Random random;

	public RocketRandom()
	{
		random = new Random();
	}

	public RocketRandom(int seed)
	{
		random = new Random(seed);
	}

	public virtual float Range(float min, float max)
	{
		double num = random.NextDouble();
		return (float)((double)min + num * (double)(max - min));
	}

	public virtual int Range(int min, int max)
	{
		return random.Next(min, max);
	}

	public virtual bool GetChance(int minRange, int maxRange)
	{
		if (Range(minRange, maxRange) == minRange)
		{
			return true;
		}
		return false;
	}
}
