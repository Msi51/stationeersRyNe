using System;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using ImGuiNET;
using UnityEngine;

public class RotatingCelestialBody : CelestialBody
{
	public bool TidallyLocked;

	public readonly Vector3 RotationAxis;

	public double _baseRotationSpeed;

	public Quaternion Rotation => Quaternion.AngleAxis(CurrentAngle, RotationAxis);

	public float DayAngle
	{
		get
		{
			double num = (double)CurrentAngle - GetWrappedTrueAnomaly();
			int num2 = 360;
			if (IsRetrogradeRotation)
			{
				num -= 180.0;
				num2 = 720;
			}
			double num3 = (num + (double)num2) % 360.0;
			if (IsRetrogradeRotation)
			{
				num3 = 360f - (float)num3;
			}
			return (float)num3;
		}
	}

	public uint DayCount
	{
		get
		{
			double val = (double)Mathf.FloorToInt((float)(double)Mathf.Abs((float)(AccumulatedAngle / 360.0))) - (double)GetStartingDayFromOrbitalEpoch();
			return (uint)Math.Max(0.0, val);
		}
	}

	public double DegreesPerPlanetYear => Orbit.Period * _baseRotationSpeed;

	public double DaysPerPlanetYear => DegreesPerPlanetYear / 360.0;

	public double SolDegreesPerDay => 360.0 / DaysPerPlanetYear;

	public double DayProgressionDegrees => _trueAnomaly % SolDegreesPerDay;

	public double DayProgressionPercent => DayProgressionDegrees / SolDegreesPerDay;

	public double DayProgressionAngle => DayProgressionPercent * 360.0;

	public bool IsRetrogradeRotation => _baseRotationSpeed < -1.401298464324817E-45;

	public float CurrentAngle
	{
		get
		{
			if (IsRetrogradeRotation)
			{
				return (float)((360.0 - DayProgressionAngle % 360.0) % 360.0);
			}
			return (float)(DayProgressionAngle % 360.0);
		}
	}

	public double AccumulatedAngle
	{
		get
		{
			double num = SolDegreesPerDay;
			if (GetRotatingReferenceBody() != this)
			{
				num = TidallyLockedMoonSolarDegreesPerDay();
			}
			double num2 = GetRotatingReferenceBody()._trueAnomaly / num;
			double num3 = 360.0 - num;
			float valueOrDefault = (_bodyTemplate?.SunriseOffset?.Value).GetValueOrDefault();
			return num2 * num3 - (double)valueOrDefault;
		}
	}

	public TimeLength GetDayLength()
	{
		return new TimeLength(360.0 / Math.Abs(_baseRotationSpeed) * 86400.0);
	}

	public TimeLength GetGameSiderealDayLength(double timeScale)
	{
		return new TimeLength(GetGameSiderealDay(timeScale));
	}

	public double GetGameSiderealDay(double timeScale)
	{
		double baseRotationSpeed = _baseRotationSpeed;
		baseRotationSpeed *= timeScale;
		double num = 360.0 / Math.Abs(baseRotationSpeed);
		CelestialBody celestialBody = base.OrbitingBody as CelestialBody;
		while (celestialBody?.OrbitingBody is CelestialBody celestialBody2)
		{
			celestialBody = celestialBody2;
		}
		if (celestialBody == null)
		{
			celestialBody = this;
		}
		double num2 = celestialBody.Orbit.Period * 86400.0 * timeScale;
		double num3 = 360.0 * (num / num2);
		return (360.0 - num3) / Math.Abs(baseRotationSpeed);
	}

	public TimeLength GetGameSiderealDayLength()
	{
		return GetGameSiderealDayLength(Simulation.TimeScale);
	}

	public TimeLength GetSiderealYearLength()
	{
		double num = GetDayLength().ToTotalSeconds();
		double num2 = GetGameSiderealDayLength().ToTotalSeconds() / num;
		CelestialBody celestialBody = base.OrbitingBody as CelestialBody;
		while (celestialBody?.OrbitingBody is CelestialBody celestialBody2)
		{
			celestialBody = celestialBody2;
		}
		if (celestialBody == null)
		{
			celestialBody = this;
		}
		return new TimeLength(celestialBody.Orbit.Period * 86400.0 * num2);
	}

	public double TidallyLockedMoonSolarDegreesPerDay()
	{
		double num = GetRotatingReferenceBody().Orbit.Period / Orbit.Period;
		return 360.0 / num;
	}

	public int GetStartingDayFromOrbitalEpoch()
	{
		if (GetRotatingReferenceBody() != this)
		{
			return Mathf.Abs(Mathf.FloorToInt((float)GetRotatingReferenceBody().LongitudeAtEpoch / (float)TidallyLockedMoonSolarDegreesPerDay()));
		}
		return Mathf.Abs(Mathf.FloorToInt((float)base.LongitudeAtEpoch / (float)SolDegreesPerDay));
	}

	public double RotationalDegreesForSiderealDay()
	{
		if (GetRotatingReferenceBody() != this)
		{
			return 360.0 - TidallyLockedMoonSolarDegreesPerDay();
		}
		return 360.0 - SolDegreesPerDay;
	}

	public RotatingCelestialBody GetRotatingReferenceBody()
	{
		RotatingCelestialBody rotatingCelestialBody = base.OrbitingBody as RotatingCelestialBody;
		while (rotatingCelestialBody?.OrbitingBody is RotatingCelestialBody rotatingCelestialBody2)
		{
			rotatingCelestialBody = rotatingCelestialBody2;
		}
		if (rotatingCelestialBody == null)
		{
			rotatingCelestialBody = this;
		}
		return rotatingCelestialBody;
	}

	public override void ApplyTo(CelestialPrefab celestialPrefab)
	{
		Vector3d scenePosition = Simulation.PlayerBody.GetScenePosition(Position);
		celestialPrefab.Transform.SetPositionAndRotation(scenePosition.ToVector3(), Simulation.GetSceneRotation() * Rotation);
	}

	public RotatingCelestialBody(OrbitalSimulation simulation, CelestialBodyTemplate bodyTemplate, Vector3 rotationAxis, double baseRotationSpeed)
		: base(simulation, bodyTemplate)
	{
		RotationAxis = rotationAxis;
		_baseRotationSpeed = baseRotationSpeed;
	}

	public override void DrawOverhead(Vector2 centerPos, Celestial referenceBody)
	{
		base.DrawOverhead(centerPos, referenceBody);
		centerPos -= referenceBody.GetOverheadPosition();
		Vector2 vector = GetOverheadPosition() + centerPos;
		float num = Mathf.Lerp(1f, 3f, OrbitalSimulation.DebugScale);
		if (SkyBoxController.OrbitDrawMode == OrbitDrawMode.LocalMap)
		{
			num *= 2f;
		}
		Vector3 vector2 = Rotation * Vector3.forward * (num * 15f);
		Vector2 vector3 = new Vector2(vector2.x, vector2.z);
		ImGui.GetForegroundDrawList().AddLine(vector, vector + vector3, ImGuiColor.Integer.Blue, 2f);
	}
}
