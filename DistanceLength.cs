using System;
using System.Text;

public readonly struct DistanceLength
{
	private readonly double au;

	public double AstronomicalUnits => au;

	public double Kilometers => au * 149597870.7;

	public DistanceLength(double au)
	{
		this.au = au;
	}

	public static DistanceLength FromAstronomicalUnits(double au)
	{
		return new DistanceLength(au);
	}

	public static DistanceLength FromKilometers(double km)
	{
		return new DistanceLength(km / 149597870.7);
	}

	public string ToNearestString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		double num = au;
		if (num < 0.1)
		{
			if (!(num < 1E-05))
			{
				if (num < 0.001)
				{
					double value = au * 149597870.7;
					stringBuilder.Append(Math.Round(value, 3)).Append(" Km");
					return stringBuilder.ToString();
				}
				double value2 = au * 149597870.7 / 1000.0;
				stringBuilder.Append(Math.Round(value2, 3)).Append(" Gm");
				return stringBuilder.ToString();
			}
			double a = au * 149597870.7 * 1000.0;
			stringBuilder.Append(Math.Round(a)).Append(" m");
			return stringBuilder.ToString();
		}
		if (num < 10000.0)
		{
			stringBuilder.Append(Math.Round(au, 3)).Append(" AU");
			return stringBuilder.ToString();
		}
		double value3 = au * 1.5812501680078302E-05;
		stringBuilder.Append(Math.Round(value3, 3)).Append(" LY");
		return stringBuilder.ToString();
	}
}
