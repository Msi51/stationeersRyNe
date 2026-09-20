using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using TerrainSystem;
using TerrainSystem.Lods;
using UnityEngine;

public static class Explosion
{
	private const int MAX_EXPLOSION_COLLIDERS = 1000;

	private static readonly Collider[] Results = new Collider[1000];

	private const int MIN_RADIUS = 1;

	private const float MAX_DAMAGE_FACTOR = 100f;

	private const float REMOVE_ALL_THRESHOLD = 0.8f;

	public static void Explode(float force, Vector3 pos, float radius, float maxDamage = float.MaxValue, bool mineTerrain = false)
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		maxDamage = Mathf.Min(force * 100f, maxDamage);
		Room room = RoomController.World.GetRoom(pos);
		if (room != null && room.RoomType == RoomType.Hydroponics)
		{
			Achievements.AchieveYesItExplodedMark();
		}
		EffectManager.CreateExplosionEffect(pos, radius);
		Vector3Int vector3Int = pos.FloorToInt();
		int num = Mathf.Max(1, (int)radius);
		float damageApplied = 0f;
		for (int i = 1; i < num && DamageThingsInSphere(force, pos, i, maxDamage, radius, ref damageApplied, mineTerrain); i++)
		{
		}
		if (mineTerrain)
		{
			Dictionary<Vector3Int, float> sphereOffsets = GetSphereOffsets(num);
			for (int j = -num; j <= num; j++)
			{
				for (int k = -num; k <= num; k++)
				{
					for (int l = -num; l <= num; l++)
					{
						if (sphereOffsets.TryGetValue(new Vector3Int(j, k, l), out var value))
						{
							Vector3Int vector3Int2 = new Vector3Int(vector3Int.x + j, vector3Int.y + k, vector3Int.z + l);
							float density = VoxelTerrain.GetDensityWorldSpace(vector3Int2) - value;
							VoxelTerrain.SetDensityWorldSpace(vector3Int2, density, RoomChangeSource.VoxelRemove, dirtyLods: false);
							if (Vein.ShouldReleaseMinables(VoxelTerrain.DensityToByte(density)))
							{
								Vein.GetVeinAtPosition(vector3Int2)?.TryMineServer(vector3Int2, out Ore _, vector3Int2);
							}
						}
					}
				}
			}
			LodManager.Instance.DirtyLodsBounds(new Vector3(vector3Int.x - num, vector3Int.y - num, vector3Int.z - num), new Vector3(vector3Int.x + num, vector3Int.y + num, vector3Int.z + num));
		}
		if (NetworkManager.IsServer && NetworkServer.HasClients())
		{
			ExplosionEvent.NewEvents.Add(new ExplosionEvent(pos, radius));
		}
	}

	private static Dictionary<Vector3Int, float> GetSphereOffsets(int radius)
	{
		Dictionary<Vector3Int, float> dictionary = new Dictionary<Vector3Int, float>();
		for (int i = -radius; i <= radius; i++)
		{
			for (int j = -radius; j <= radius; j++)
			{
				for (int k = -radius; k <= radius; k++)
				{
					Vector3Int key = new Vector3Int(i, j, k);
					float magnitude = key.magnitude;
					if (!(magnitude > (float)radius))
					{
						float num = magnitude / (float)radius;
						float num2 = Mathf.Pow(1f - num, 0.4f);
						if (num2 > 0.8f)
						{
							num2 = 1f;
						}
						dictionary.TryAdd(key, num2);
					}
				}
			}
		}
		return dictionary;
	}

	public static void DamageThing(Thing thing, Vector3 position, float force)
	{
		thing.DamageState.Damage(ChangeDamageType.Increment, force, DamageUpdateType.Brute);
		thing.DamageState.Damage(ChangeDamageType.Increment, force, DamageUpdateType.Stun);
		thing.Explosion(position, force);
	}

	private static bool DamageThingsInSphere(float force, Vector3 position, int radius, float maxDamage, float maxRadius, ref float damageApplied, bool mineTerrain)
	{
		int num = Physics.OverlapSphereNonAlloc(position, radius, Results);
		for (int i = 0; i < num; i++)
		{
			if (damageApplied > maxDamage)
			{
				return false;
			}
			Thing thing = Thing.Find(Results[i]);
			if ((!mineTerrain || (!(thing is IExplosive) && !(thing is Ore))) && !(thing is Structure { IsBroken: not false }) && (object)thing != null)
			{
				float num2 = Mathf.Lerp(force, 0f, Vector3.Distance(thing.Position, position) / maxRadius);
				DamageThing(thing, position, num2);
				damageApplied += num2;
			}
		}
		return true;
	}
}
