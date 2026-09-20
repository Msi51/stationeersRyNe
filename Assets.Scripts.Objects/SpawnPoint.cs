using System;
using TerrainSystem;
using TerrainSystem.Lods;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class SpawnPoint : Thing, ISpawnPoint, IReferencable, IEvaluable
{
	public bool AddOnSpawn = true;

	private static readonly float _maxSpawnDis = 80f;

	private static readonly float _checkSphereRadius = 2f;

	private static readonly float _spawnDisIncrement = 0.1f;

	private const int MAX_ITERATIONS = 50;

	private const float RAYCAST_OFFSET = 100f;

	private static Vector3 EstimateTerrainSurface(Vector3 startPoint)
	{
		float y = LodManager.Instance.TerrainHeight.y;
		int num = (int)(LodManager.Instance.TerrainHeight.y - LodManager.Instance.TerrainHeight.x);
		Vector3 vector = new Vector3(startPoint.x, y, startPoint.z);
		Vector3 vector2 = vector;
		for (int i = 0; i < num; i++)
		{
			if ((double)VoxelTerrain.GetDensityWorldSpace(vector2) > 0.5)
			{
				return vector2;
			}
			vector2 += Vector3.down;
		}
		return vector;
	}

	public static Vector3 GetSafePoint(Vector3 position)
	{
		return GetSafePoint(position, Vector3.zero);
	}

	public static Vector3 GetSafePoint(Vector3 position, Vector3 offset, bool avoidStructure = false)
	{
		if (ThreadedManager.IsThread)
		{
			ConsoleWindow.PrintError("GetSafePoint can only be called from the main thread");
			return Vector3.zero;
		}
		float num = 0f;
		for (int i = 0; i < 50; i++)
		{
			position = EstimateTerrainSurface(position);
			if (WorldSetting.Current.IsUnderLava(position))
			{
				num += 1f;
				offset = new Vector3(4f * Mathf.Cos(num), 0f, 4f * Mathf.Sin(num));
				position += offset;
				continue;
			}
			if (!Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out var hitInfo, 200f))
			{
				break;
			}
			Thing thing = Thing.Find(hitInfo.collider);
			position = new Vector3(position.x, Mathf.Max(position.y, hitInfo.point.y), position.z);
			if (!(thing is Lander) && !(thing is LanderCapsule) && !(thing is LanderCapsuleDoor) && !(thing is Structure && avoidStructure))
			{
				break;
			}
			num += 1f;
			offset = new Vector3(4f * Mathf.Cos(num), 0f, 4f * Mathf.Sin(num));
			position += offset;
		}
		return position + new Vector3(0f, 4f, 0f);
	}

	public new Thing GetThing()
	{
		return this;
	}

	public Transform GetSpawnPointTransform()
	{
		return ThingTransform;
	}

	public static void AssignSpawn(ulong steamId, ISpawnPoint spawnPoint)
	{
		if (!GameManager.RunSimulation)
		{
			throw new Exception("Client cannot assign spawn");
		}
		SerializedClientInfo.UpdateSpawnPointReference(steamId, spawnPoint.ReferenceId);
	}

	public override void Start()
	{
		base.Start();
		ThingTransform = base.transform;
		if (!IsCursor && AddOnSpawn)
		{
			IgnoreSave = true;
		}
	}
}
