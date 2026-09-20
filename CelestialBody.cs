using System;
using Assets.Scripts;
using Assets.Scripts.UI.ImGuiUi;
using ImGuiNET;
using UnityEngine;

public class CelestialBody : Celestial
{
	protected internal double _trueAnomaly;

	public Orbit Orbit;

	protected readonly CelestialBodyTemplate _bodyTemplate;

	public double UnscaledDeltaTime = 1.0;

	public Celestial OrbitingBody { get; set; }

	public double LongitudeAtEpoch => _bodyTemplate.LongitudeAtEpoch;

	public CelestialBody(OrbitalSimulation simulation, CelestialBodyTemplate bodyTemplate)
	{
		string id = bodyTemplate.Id;
		LocalizedStringReference name = bodyTemplate.Name;
		base._002Ector(id, (name != null) ? ((string)name) : bodyTemplate.Id, simulation);
		_bodyTemplate = bodyTemplate;
		Orbit = new Orbit(bodyTemplate.OrbitData);
		_trueAnomaly = bodyTemplate.LongitudeAtEpoch;
	}

	public override void Set(double timeDelta)
	{
		double num = Simulation.PlayerBody.Orbit.Period / Orbit.Period * timeDelta;
		_trueAnomaly = LongitudeAtEpoch + num;
		Position = GetPosition(_trueAnomaly);
		base.Set(timeDelta);
	}

	public Vector3d GetPosition(double orbitAngle)
	{
		return OrbitingBody.Position + Orbit.GetLocalPosition(orbitAngle);
	}

	public override void DrawCircleDebug(Vector2 position, float drawScale)
	{
		ImGui.GetForegroundDrawList().AddCircleFilled(position, drawScale + 1f, ImGuiColor.Integer.Green, Mathf.RoundToInt(drawScale * 16f) + 3);
	}

	public override void Initialize(CelestialBody playerBody)
	{
		RangeToPlayer = CalculateDistanceRange(playerBody);
		EclipticInclination = CalculateEclipticInclination();
	}

	private static Vector2 ToScreenCoords(Vector2 worldPoint)
	{
		float x = worldPoint.x * OrbitalSimulation.DebugSystemScale;
		float y = (float)Screen.height - worldPoint.y * OrbitalSimulation.DebugSystemScale;
		return new Vector2(x, y);
	}

	public override void DrawOverhead(Vector2 centerPos, Celestial referenceBody)
	{
		base.DrawOverhead(centerPos, referenceBody);
		centerPos -= referenceBody.GetOverheadPosition();
		int num = 360;
		Vector2? vector = null;
		for (int i = 0; i <= num; i++)
		{
			double orbitAngle = 360.0 / (double)num * (double)i;
			Vector3d position = GetPosition(orbitAngle);
			Vector2 vector2 = ToScreenCoords(new Vector2((float)position.x, (float)position.z)) + centerPos;
			if (vector.HasValue)
			{
				ImGui.GetForegroundDrawList().AddLine(vector.Value, vector2, ImGuiColor.Integer.White, 1f);
			}
			vector = vector2;
		}
	}

	public double GetTotalOrbitRadius()
	{
		double num = Orbit.SemiMajorAxis;
		for (CelestialBody celestialBody = OrbitingBody as CelestialBody; celestialBody != null; celestialBody = OrbitalSimulation.GetPrimaryBody() as CelestialBody)
		{
			num += celestialBody.Orbit.SemiMajorAxis;
		}
		return num;
	}

	public float CalculateEclipticInclination()
	{
		float num = Orbit.Inclination;
		if (OrbitingBody is CelestialBody celestialBody)
		{
			num += celestialBody.CalculateEclipticInclination();
		}
		if (num > 180f)
		{
			num = 360f - num;
		}
		return num;
	}

	public DistanceRange GetRangeToPrimaryBody()
	{
		DistanceRange minMaxDistance = Orbit.GetMinMaxDistance();
		double num = minMaxDistance.Minimum;
		double num2 = minMaxDistance.Maximum;
		Celestial orbitingBody = OrbitingBody;
		while (orbitingBody != null && orbitingBody is CelestialBody celestialBody)
		{
			DistanceRange minMaxDistance2 = celestialBody.Orbit.GetMinMaxDistance();
			num += minMaxDistance2.Minimum;
			num2 += minMaxDistance2.Maximum;
			orbitingBody = celestialBody.OrbitingBody;
		}
		return new DistanceRange(num, num2);
	}

	private DistanceRange CalculateDistanceRange(CelestialBody otherBody)
	{
		double num;
		double num2;
		if (otherBody.OrbitingBody == OrbitingBody)
		{
			num = Orbit.SemiMajorAxis;
			num2 = otherBody.Orbit.SemiMajorAxis;
		}
		else
		{
			num = GetTotalOrbitRadius();
			num2 = otherBody.GetTotalOrbitRadius();
		}
		double min;
		double max;
		if (num > num2)
		{
			min = num - num2;
			max = num + num2;
		}
		else
		{
			min = num2 - num;
			max = num2 + num;
		}
		return new DistanceRange(min, max);
	}

	public double GetWrappedTrueAnomaly()
	{
		return _trueAnomaly % 360.0;
	}

	public uint GetOrbitProgressionCount()
	{
		return (uint)Math.Floor(Math.Abs(_trueAnomaly) / 360.0);
	}

	public override void PrintDebug()
	{
		TreeString treeString = new TreeString(GetType().Name + ": " + Name);
		TreeString.Variable($"Eccliptic Inclination: {EclipticInclination:F2}°", treeString);
		TreeString.Variable("Distance to Player: " + GetNearestUnitDistance(), treeString);
		TreeString.Variable($"Range to Player: {RangeToPlayer.Minimum:F2} to {RangeToPlayer.Maximum:F2} AU", treeString);
		if (OrbitingBody != null)
		{
			TreeString treeString2 = TreeString.Node("Orbiting " + OrbitingBody.Name, treeString);
			TreeString.Variable($"True Anomaly: {GetWrappedTrueAnomaly():F2}°", treeString2);
			TreeString.Variable($"Progression Count: {GetOrbitProgressionCount()}", treeString2);
			Orbit.PrintDebug(treeString2);
		}
		treeString.ToConsole();
	}
}
