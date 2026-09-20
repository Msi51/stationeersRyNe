using System;

public class MovingAverage
{
	private readonly int _k;

	private readonly double[] _values;

	private int _index;

	private double _sum;

	public MovingAverage(int k)
	{
		if (k <= 0)
		{
			throw new ArgumentOutOfRangeException("k", "Must be greater than 0");
		}
		_k = k;
		_values = new double[k];
	}

	public double Update(double nextInput)
	{
		_sum = _sum - _values[_index] + nextInput;
		_values[_index] = nextInput;
		_index = (_index + 1) % _k;
		return _sum / (double)_k;
	}
}
