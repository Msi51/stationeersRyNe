using UnityEngine;

public readonly struct CelestialHit(Celestial celestial, float angle)
{
	public static readonly CelestialHit INVALID = new CelestialHit(null, float.MaxValue);

	public readonly Celestial Celestial = celestial;

	public readonly float Angle = Mathf.Abs(angle);

	public double GetLogicAngle()
	{
		if (Celestial == null)
		{
			return double.NaN;
		}
		return Angle;
	}

	public double GetDistanceAu()
	{
		if (Celestial == null)
		{
			return double.NaN;
		}
		return Celestial.DistanceToPlayer;
	}

	public double GetDistanceKm()
	{
		if (Celestial == null)
		{
			return double.NaN;
		}
		return Celestial.DistanceToPlayer * 149597870.7;
	}

	public double GetPeriodDays()
	{
		if (!(Celestial is CelestialBody celestialBody))
		{
			return double.NaN;
		}
		return celestialBody.Orbit.Period;
	}

	public float GetInclination()
	{
		if (!(Celestial is CelestialBody celestialBody))
		{
			return float.NaN;
		}
		return celestialBody.Orbit.Inclination;
	}

	public float GetEccentricity()
	{
		if (!(Celestial is CelestialBody celestialBody))
		{
			return float.NaN;
		}
		return celestialBody.Orbit.Eccentricity;
	}

	public double GetSemiMajorAxis()
	{
		if (!(Celestial is CelestialBody celestialBody))
		{
			return double.NaN;
		}
		return celestialBody.Orbit.SemiMajorAxis;
	}

	public int GetParentHash()
	{
		if (!(Celestial is CelestialBody celestialBody))
		{
			return 0;
		}
		return celestialBody.OrbitingBody?.Hash ?? 0;
	}

	public double GetTrueAnomaly()
	{
		if (!(Celestial is CelestialBody celestialBody))
		{
			return 0.0;
		}
		return celestialBody.GetWrappedTrueAnomaly();
	}
}
