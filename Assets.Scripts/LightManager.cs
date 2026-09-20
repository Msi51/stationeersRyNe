using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.Util;
using TerrainSystem;
using UnityEngine;
using Weather;

namespace Assets.Scripts;

public class LightManager : ThreadedManager
{
	public enum DebugMode
	{
		Off,
		Cells,
		Atmospheres
	}

	public static readonly float GridSurfaceArea = 24f;

	public static bool SunPathTraceWorldAtmos = false;

	public DebugMode LightDebugMode;

	public static LightManager Instance;

	public static readonly float LightCastDistance = 800f;

	private const int MAX_LIGHT_ACTIVATED = 16384;

	public static readonly ConcurrentDensePool<ILightActivated> AllLightActivated = new ConcurrentDensePool<ILightActivated>("AllLightActivated", 16384);

	private static readonly Action<ILightActivated> AllLightActivatedAction = delegate(ILightActivated lightActivated)
	{
		if (lightActivated != null && !lightActivated.IsBeingDestroyed)
		{
			if (!(lightActivated is Device device))
			{
				if (!(lightActivated is HydroponicTray hydroponicTray))
				{
					if (!(lightActivated is DynamicThing dynamicThing))
					{
						throw new NotImplementedException($"LightActivated type {lightActivated.GetType()} is not implemented.");
					}
					CheckLightOcclusion(dynamicThing);
				}
				else if (CheckLightOcclusion(hydroponicTray) != hydroponicTray.HasLight)
				{
					if (hydroponicTray.HasLight)
					{
						if ((bool)hydroponicTray.Plant)
						{
							hydroponicTray.Plant.HasLight = true;
						}
						hydroponicTray.OnHasSunlight().Forget();
					}
					else
					{
						if ((bool)hydroponicTray.Plant)
						{
							hydroponicTray.Plant.HasLight = false;
						}
						hydroponicTray.OnLostSunlight().Forget();
					}
				}
			}
			else if (CheckLightOcclusion(device) != device.HasLight)
			{
				if (device.HasLight)
				{
					device.OnHasSunlight().Forget();
				}
				else
				{
					device.OnLostSunlight().Forget();
				}
			}
		}
	};

	public static Vector3 LightRay;

	public LayerMask WallLightsMask;

	public static float AirSolarIrradiance
	{
		get
		{
			float num = ((WeatherManager.CurrentWeatherEvent != null && WeatherManager.IsWeatherEventRunning) ? ((float)WeatherManager.CurrentWeatherEvent.SolarRatio) : 1f);
			return 0.05f * OrbitalSimulation.SolarIrradiance * num;
		}
	}

	protected override System.Threading.ThreadPriority ThreadPriority => Settings.NonFrameCriticalThreadPriority;

	public static float SolarHeatingCurve(float atmosTempK)
	{
		return 1f / (1f + Mathf.Pow(atmosTempK / 450f, 3.2f));
	}

	public override void ManagerAwake()
	{
		base.ManagerAwake();
		if (Instance == null)
		{
			Instance = this;
		}
	}

	public static void Register(ILightActivated dynamicThing)
	{
		if (GameManager.GameState != GameState.None)
		{
			AllLightActivated.Add(dynamicThing);
		}
	}

	public static void Deregister(ILightActivated dynamicThing)
	{
		AllLightActivated.Remove(dynamicThing);
	}

	public static void ClearAll()
	{
		AllLightActivated.Clear();
	}

	public override void ThreadedWork()
	{
		base.ThreadedWork();
		try
		{
			if (GameManager.GameState != GameState.Running)
			{
				return;
			}
			LightRay = OrbitalSimulation.WorldSunVector * LightCastDistance;
			foreach (KeyValuePair<Cell, byte> allCell in Cell.AllCells)
			{
				if (allCell.Key != null)
				{
					CheckLightOcclusion(allCell.Key);
				}
			}
			AllLightActivated.ForEach(AllLightActivatedAction);
		}
		catch (Exception ex)
		{
			string text = "Exception: " + ex.Message + "\n";
			string stackTrace = ex.StackTrace;
			for (int i = 0; i < stackTrace.Length; i++)
			{
				text += stackTrace[i];
			}
			Debug.LogError(text);
			ConsoleWindow.PrintError("Lighting Exception.</b> " + ex.Message + "</color>");
		}
	}

	public static bool CheckLightOcclusion(Cell gridCell)
	{
		gridCell.HasLight = !gridCell.IsBlockedLight && HasSunlight(gridCell);
		return gridCell.HasLight;
	}

	public static bool CheckLightOcclusion(Device device)
	{
		return device.HasLight = HasSunlight(device.GridController.LocalToWorld(device.LocalGrid));
	}

	public static bool CheckLightOcclusion(Structure structure)
	{
		return structure.HasLight = HasSunlight(structure.GridController.LocalToWorld(structure.LocalGrid));
	}

	public static bool CheckLightOcclusion(DynamicThing dynamicThing)
	{
		dynamicThing.HasLight = HasSunlight(dynamicThing.GridController.LocalToWorld(dynamicThing.WorldGrid));
		return dynamicThing.HasLight;
	}

	public static void CheckLightOcclusion(Atmosphere atmosphere)
	{
		if (atmosphere.Mode == AtmosphereHelper.AtmosphereMode.Thing && (!atmosphere.Thing || ((bool)atmosphere.Thing.AsDynamicThing && atmosphere.Thing.AsDynamicThing.IsChild && atmosphere.Thing.AsDynamicThing.IsHiddenInParentSlot())))
		{
			atmosphere.HasLight = false;
		}
		else
		{
			atmosphere.HasLight = ((atmosphere.Cell != null) ? atmosphere.Cell.HasLight : HasSunlight(atmosphere.WorldPosition));
		}
	}

	public static bool HasSunlight(Cell cell)
	{
		if (OrbitalSimulation.IsEclipse)
		{
			return false;
		}
		if (cell.IsBlockedLight)
		{
			return false;
		}
		Vector3 position = cell.Position;
		Vector3 comparePosition = cell.Position + LightRay;
		Vector3 closestFace = RocketGrid.GetClosestFace(position, comparePosition);
		if (!cell.IsOpenLight(closestFace))
		{
			return false;
		}
		return HasSunlight(cell.Position);
	}

	public static bool HasSunlight(Vector3 gridCell)
	{
		if (OrbitalSimulation.IsEclipse)
		{
			return false;
		}
		Vector3 lineEnd = gridCell + LightRay;
		Vector3 vector = Vector3.one * 0.5f;
		float x = vector.x;
		float y = vector.y;
		float z = vector.z;
		float x2 = LightRay.x;
		float y2 = LightRay.y;
		float z2 = LightRay.z;
		float num = Mathf.Floor(x);
		float num2 = Mathf.Floor(y);
		float num3 = Mathf.Floor(z);
		float num4 = Mathf.Floor(x2);
		float num5 = Mathf.Floor(y2);
		float num6 = Mathf.Floor(z2);
		int num7 = ((num4 > num) ? 1 : ((num4 < num) ? (-1) : 0));
		int num8 = ((num5 > num2) ? 1 : ((num5 < num2) ? (-1) : 0));
		int num9 = ((num6 > num3) ? 1 : ((num6 < num3) ? (-1) : 0));
		float num10 = num;
		float num11 = num2;
		float num12 = num3;
		float num13 = num + (float)((num4 > num) ? 1 : 0);
		float num14 = num2 + (float)((num5 > num2) ? 1 : 0);
		float num15 = num3 + (float)((num6 > num3) ? 1 : 0);
		float num16 = ((x2 == x) ? 1f : (x2 - x));
		float num17 = ((y2 == y) ? 1f : (y2 - y));
		float num18 = ((z2 == z) ? 1f : (z2 - z));
		float num19 = num16 * num17;
		float num20 = num16 * num18;
		float num21 = num17 * num18;
		float num22 = (num13 - x) * num21;
		float num23 = (num14 - y) * num20;
		float num24 = (num15 - z) * num19;
		float num25 = (float)num7 * num21;
		float num26 = (float)num8 * num20;
		float num27 = (float)num9 * num19;
		int num28 = 0;
		Vector3 lastPosition = gridCell;
		while (Instance != null && Instance.IsRunning)
		{
			num28++;
			if (num28 > 1000)
			{
				break;
			}
			Vector3 vector2 = new Vector3(num10, num11, num12) * 2f + gridCell;
			if (!CanSee(gridCell, lineEnd, vector2.GridCenter(), lastPosition))
			{
				return false;
			}
			lastPosition = vector2;
			if (num10 == num4 && num11 == num5 && num12 == num6)
			{
				break;
			}
			float num29 = Mathf.Abs(num22);
			float num30 = Mathf.Abs(num23);
			float num31 = Mathf.Abs(num24);
			if (num7 != 0 && (num8 == 0 || num29 < num30) && (num9 == 0 || num29 < num31))
			{
				num10 += (float)num7;
				num22 += num25;
			}
			else if (num8 != 0 && (num9 == 0 || num30 < num31))
			{
				num11 += (float)num8;
				num23 += num26;
			}
			else if (num9 != 0)
			{
				num12 += (float)num9;
				num24 += num27;
			}
		}
		return true;
	}

	public static bool IsVoxelLightBlocked(Vector3 grid)
	{
		if (VoxelTerrain.GetDensityAtSize(grid, 2) > 0.49803922f)
		{
			return true;
		}
		return false;
	}

	public static bool CanSee(Vector3 lineStart, Vector3 lineEnd, Vector3 grid, Vector3 lastPosition)
	{
		if (IsVoxelLightBlocked(grid))
		{
			return false;
		}
		Cell cell = GridController.World.GetCell(grid);
		if (cell == null)
		{
			return true;
		}
		if (cell.IsBlockedLight)
		{
			return false;
		}
		Vector3 closestFace = RocketGrid.GetClosestFace(cell.Position, lastPosition);
		Vector3 closestFace2 = RocketGrid.GetClosestFace(cell.Position, lineStart);
		Vector3 closestFace3 = RocketGrid.GetClosestFace(cell.Position, lineEnd);
		if (!cell.IsOpenLight(closestFace2) || !cell.IsOpenLight(closestFace3) || !cell.IsOpenLight(closestFace))
		{
			return false;
		}
		return true;
	}
}
