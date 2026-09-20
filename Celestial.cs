using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using ImGuiNET;
using UI.ImGuiUi;
using UnityEngine;

public class Celestial
{
	public OrbitalSimulation Simulation;

	public const string DEFAULT_STAR_ID = "Sol";

	public const string DEFAULT_STAR_NAME = "CelestialBodySol";

	public const float MINIMUM_MAGNITUDE = 5f;

	public const float MAXIMUM_MAGNITUDE = 0.02f;

	public const double Deg2Rad = Math.PI / 180.0;

	public const double Rad2Deg = 180.0 / Math.PI;

	public List<CelestialBody> Bodies = new List<CelestialBody>();

	public string Id;

	public string Name;

	public Vector3d Position;

	public DistanceRange RangeToPlayer = DistanceRange.zero;

	public float EclipticInclination;

	protected const int SEGMENTS_PER_32_PIXELS = 4;

	public CelestialPrefab Prefab { get; set; }

	public int Hash { get; private set; }

	public Vector3 WorldVector { get; private set; }

	public double DistanceToPlayer { get; private set; }

	public Celestial(string id, string name, OrbitalSimulation simulation)
	{
		Id = id;
		Name = name;
		Hash = Animator.StringToHash(id);
		Simulation = simulation;
	}

	public virtual void PrintDebug()
	{
		TreeString treeString = new TreeString(GetType().Name + ": " + Name);
		TreeString.Variable($"Range to Player: {RangeToPlayer.Minimum:F2} to {RangeToPlayer.Maximum:F2} AU", treeString);
		treeString.ToConsole();
	}

	public double DistanceTo(Celestial celestial)
	{
		if (celestial == null)
		{
			return double.PositiveInfinity;
		}
		return Vector3d.Distance(celestial.Position, Position);
	}

	public void SetPlayerVectorTo(RotatingCelestialBody playerBody)
	{
		WorldVector = playerBody.GetSceneLocalPosition(Position).normalized.ToVector3();
		DistanceToPlayer = (float)DistanceTo(playerBody);
	}

	public void ProjectTo(CelestialPrefab celestialPrefab)
	{
		Vector3d vector = Simulation.PlayerBody.GetSceneLocalPosition(Position).normalized * 200.0;
		vector += CameraController.CameraPosition;
		celestialPrefab.Transform.position = vector.ToVector3();
		celestialPrefab.Transform.LookAt(CameraController.CameraPosition);
	}

	public virtual void ApplyTo(CelestialPrefab celestialPrefab)
	{
		Vector3d scenePosition = Simulation.PlayerBody.GetScenePosition(Position);
		celestialPrefab.Transform.SetPositionAndRotation(scenePosition.ToVector3(), Quaternion.identity);
	}

	public Vector3d GetScenePosition(Vector3d position)
	{
		return GetSceneLocalPosition(position) + CameraController.CameraPosition;
	}

	public Vector3d GetSceneLocalPosition(Vector3d position)
	{
		Vector3d vector3d = Position - position;
		vector3d *= (double)(OrbitalSimulation.SkyboxScale * 10000f);
		return Simulation.GetSceneRotation() * vector3d;
	}

	public virtual void Set(double timeDelta)
	{
		foreach (CelestialBody body in Bodies)
		{
			body.Set(timeDelta);
		}
	}

	public void DrawDebug(RotatingCelestialBody playerBody, Celestial referenceBody)
	{
		if (playerBody == null)
		{
			return;
		}
		Vector2 centerPos = new Vector2((float)Screen.width * 0.5f, (float)Screen.height * 0.5f);
		switch (SkyBoxController.OrbitDrawMode)
		{
		case OrbitDrawMode.SystemMap:
		case OrbitDrawMode.LocalMap:
			DrawOverhead(centerPos, referenceBody);
			break;
		case OrbitDrawMode.ProfileMap:
			DrawProfile(centerPos, referenceBody);
			break;
		case OrbitDrawMode.InWorld:
		{
			if (playerBody == this)
			{
				break;
			}
			Vector2 screenPosition = OrbitalSimulation.GetScreenPosition(playerBody.GetScenePosition(Position), CameraController.CurrentCamera);
			if (!(screenPosition == Vector2.negativeInfinity))
			{
				if (SkyBoxController.OrbitRenderSetting == OrbitRenderSetting.NamesAndMarkers)
				{
					ImGui.GetForegroundDrawList().AddCircleFilled(screenPosition, 5f, ImGuiColor.Integer.Green, 8);
				}
				screenPosition += Vector2.right * 14f;
				ImguiHelper.DrawText(Name, screenPosition, Name ?? "", ImGuiColor.Float4.White);
				screenPosition -= Vector2.down * ImGui.GetTextLineHeightWithSpacing();
				ImguiHelper.DrawText(Name + "Distance", screenPosition, $"{GetNearestUnitDistance()} {RangeToPlayer.Ratio(DistanceToPlayer):P0}", ImGuiColor.Float4.Grey);
			}
			break;
		}
		}
		foreach (CelestialBody body in Bodies)
		{
			body.DrawDebug(playerBody, referenceBody);
		}
	}

	public string GetNearestUnitDistance()
	{
		return DistanceLength.FromAstronomicalUnits(DistanceToPlayer).ToNearestString();
	}

	public virtual void DrawCircleDebug(Vector2 position, float drawScale)
	{
		ImGui.GetForegroundDrawList().AddCircleFilled(position, drawScale * 2f + 1f, ImGuiColor.Integer.Yellow, Mathf.RoundToInt(drawScale * 16f) + 3);
	}

	public virtual void DrawOverhead(Vector2 centerPos, Celestial referenceBody)
	{
		centerPos -= referenceBody.GetOverheadPosition();
		Vector2 vector = GetOverheadPosition() + centerPos;
		float num = Mathf.Lerp(1f, 3f, OrbitalSimulation.DebugScale);
		if (SkyBoxController.OrbitDrawMode == OrbitDrawMode.LocalMap)
		{
			num *= 2f;
		}
		DrawCircleDebug(vector, num);
		vector += Vector2.right * 14f;
		ImguiHelper.DrawText(Name, vector, Name, ImGuiColor.Float4.White);
	}

	private void DrawProfile(Vector2 centerPos, Celestial referenceBody)
	{
		CelestialBody celestialBody = this as CelestialBody;
		centerPos -= referenceBody.GetSidePosition();
		Vector2 vector = GetSidePosition() + centerPos;
		float num = Mathf.Lerp(1f, 3f, OrbitalSimulation.DebugScale);
		if (SkyBoxController.OrbitDrawMode == OrbitDrawMode.LocalMap)
		{
			num *= 10f;
		}
		if (celestialBody != null)
		{
			float num2 = (float)(celestialBody.Orbit.SemiMajorAxis * (double)OrbitalSimulation.DebugSystemScale);
			float num3 = (float)Math.Sin((double)celestialBody.Orbit.Inclination * (Math.PI / 180.0));
			Vector2 vector2 = celestialBody.OrbitingBody.GetSidePosition() + centerPos;
			float num4 = num2;
			float num5 = num2 * num3;
			Vector2 p = Vector2.zero;
			Vector2 p2 = Vector2.zero;
			int value = Mathf.RoundToInt(2f * num2 / 32f * 4f);
			value = Mathf.Clamp(value, 8, 512);
			for (int i = 0; i < value; i++)
			{
				float f = MathF.PI * 2f / (float)value * (float)i;
				float x = num4 * Mathf.Cos(f);
				float y = num5 * Mathf.Sin(f);
				Vector2 vector3 = new Vector2(x, y) + vector2;
				if (i > 0)
				{
					ImGui.GetForegroundDrawList().AddLine(p2, vector3, ImGuiColor.Integer.White, 1f);
				}
				if (i == 0)
				{
					p = vector3;
				}
				p2 = vector3;
			}
			ImGui.GetForegroundDrawList().AddLine(p2, p, ImGuiColor.Integer.White, 1f);
		}
		DrawCircleDebug(vector, num);
		vector += Vector2.right * 14f;
		ImguiHelper.DrawText(Name, vector, Name, ImGuiColor.Float4.White);
	}

	public Vector2 GetOverheadPosition()
	{
		float x = (float)(Position.x * (double)OrbitalSimulation.DebugSystemScale);
		float y = (float)Screen.height - (float)(Position.z * (double)OrbitalSimulation.DebugSystemScale);
		return new Vector2(x, y);
	}

	public Vector2 GetSidePosition()
	{
		float x = (float)(Position.z * (double)OrbitalSimulation.DebugSystemScale);
		float y = (float)Screen.height - (float)(Position.y * (double)OrbitalSimulation.DebugSystemScale);
		return new Vector2(x, y);
	}

	public void Clear()
	{
		Bodies.Clear();
	}

	public void Add(CelestialBody body)
	{
		Bodies.Add(body);
		Simulation.Register(body);
	}

	public void Rename(string id, string name)
	{
		Id = id;
		Name = name;
		Hash = Animator.StringToHash(id);
	}

	public virtual void Initialize(CelestialBody playerBody)
	{
		DistanceToPlayer = playerBody.DistanceTo(this);
		RangeToPlayer = playerBody.GetRangeToPrimaryBody();
		EclipticInclination = 0f;
	}

	public string ToTooltip()
	{
		return "<color=green>" + Name + "</color>";
	}
}
