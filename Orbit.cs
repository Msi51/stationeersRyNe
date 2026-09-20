using System;
using Assets.Scripts;
using Assets.Scripts.Util;
using UnityEngine;

public class Orbit
{
	public double Period { get; set; }

	public float Inclination { get; set; }

	public float Eccentricity { get; set; }

	public double SemiMajorAxis { get; set; }

	public double LongitudeOfAscendingNode { get; set; }

	public double ArgumentOfPeriapsis { get; set; }

	public DistanceLength GetSemiMajorAxis()
	{
		return DistanceLength.FromAstronomicalUnits(SemiMajorAxis);
	}

	public Orbit(OrbitData orbitData)
	{
		Inclination = orbitData.Inclination;
		Eccentricity = orbitData.GetEccentricity();
		SemiMajorAxis = orbitData.GetSemiMajorAxis();
		Period = orbitData.Period;
		LongitudeOfAscendingNode = orbitData.GetLongitudeOfAscendingNode();
		ArgumentOfPeriapsis = orbitData.GetArgumentOfPeriapsis();
	}

	public DistanceRange GetMinMaxDistance()
	{
		return new DistanceRange(SemiMajorAxis * (double)(1f - Eccentricity), SemiMajorAxis * (double)(1f + Eccentricity));
	}

	public Vector3d GetLocalPosition(double currentOrbitAngleDegrees)
	{
		double meanAnomaly = currentOrbitAngleDegrees * (Math.PI / 180.0);
		double num = CalculateTrueAnomaly(meanAnomaly, Eccentricity);
		double num2 = SemiMajorAxis * (double)(1f - Eccentricity * Eccentricity) / (1.0 + (double)Eccentricity * Math.Cos(num));
		double num3 = num + ArgumentOfPeriapsis;
		double x = num2 * (Math.Cos(LongitudeOfAscendingNode) * Math.Cos(num3) - Math.Sin(LongitudeOfAscendingNode) * Math.Sin(num3) * Math.Cos((double)Inclination * (Math.PI / 180.0)));
		double y = num2 * (Math.Sin(num3) * Math.Sin((double)Inclination * (Math.PI / 180.0)));
		double z = num2 * (Math.Sin(LongitudeOfAscendingNode) * Math.Cos(num3) + Math.Cos(LongitudeOfAscendingNode) * Math.Sin(num3) * Math.Cos((double)Inclination * (Math.PI / 180.0)));
		return new Vector3d
		{
			x = x,
			y = y,
			z = z
		};
	}

	private double CalculateTrueAnomaly(double meanAnomaly, double eccentricity)
	{
		double num = meanAnomaly;
		double num2 = 0.0001;
		int num3 = 100;
		for (int i = 0; i < num3; i++)
		{
			double num4 = num - eccentricity * Math.Sin(num) - meanAnomaly;
			double num5 = 1.0 - eccentricity * Math.Cos(num);
			double num6 = num - num4 / num5;
			if (Math.Abs(num6 - num) < num2)
			{
				break;
			}
			num = num6;
		}
		return 2.0 * Math.Atan2(Math.Sqrt(1.0 + eccentricity) * Math.Sin(num / 2.0), Math.Sqrt(1.0 - eccentricity) * Math.Cos(num / 2.0));
	}

	public TimeLength GetOrbitLength()
	{
		return TimeLength.FromDays(Period);
	}

	public void PrintDebug(TreeString myString)
	{
		TreeString.Node("Period: " + GetOrbitLength().ToNearestString(), myString);
		TreeString.Node($"Inclination: {Inclination:F2}°", myString);
		TreeString.Node($"Eccentricity: {Eccentricity:F2}", myString);
		TreeString.Node($"SemiMajorAxis: {SemiMajorAxis:F4} AU", myString);
		TreeString.Node($"LongitudeOfAscendingNode: {LongitudeOfAscendingNode:F2}°", myString);
		TreeString.Node($"ArgumentOfPeriapsis: {ArgumentOfPeriapsis:F2}°", myString);
	}
}
