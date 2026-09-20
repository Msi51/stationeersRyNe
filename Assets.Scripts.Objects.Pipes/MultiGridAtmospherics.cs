using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public static class MultiGridAtmospherics
{
	public const int MAX_DEPTH = 6;

	private const int MAX_MIXING_ATMOSPHERES = 128;

	public static readonly HashSet<Atmosphere> NextShell = new HashSet<Atmosphere>(128);

	private static readonly HashSet<Atmosphere> CurrentShell = new HashSet<Atmosphere>(128);

	private static readonly HashSet<Atmosphere> PreviousShell = new HashSet<Atmosphere>(128);

	public static readonly PressurekPa DISCARD_REMAINING_THRESHOLD = new PressurekPa(0.10000000149011612);

	public static void VentFromNeighborsToPipe(Atmosphere targetAtmosphere, Atmosphere localAtmosphere, ref PressurekPa remainingToMove, int gridMixingDepth, PressurekPa externalTarget)
	{
		NextShell.Clear();
		NextShell.Add(localAtmosphere);
		int num = Mathf.Min(gridMixingDepth, 6);
		for (int i = 0; i < num; i++)
		{
			CalculateNextShell();
			VentToNextShell(targetAtmosphere, ref remainingToMove, toWorld: false, externalTarget);
			if (remainingToMove < DISCARD_REMAINING_THRESHOLD)
			{
				break;
			}
		}
	}

	public static void VentWorldToInternal(Atmosphere internalAtmosphere, List<WorldGrid> startingGrids, PressurekPa remainingToMove, int gridMixingDepth, float maxTakeRatio = 0.5f)
	{
		NextShell.Clear();
		foreach (WorldGrid startingGrid in startingGrids)
		{
			NextShell.Add(AtmosphericsController.World.CloneGlobalAtmosphere(startingGrid, 0L));
		}
		VentToNextShell(internalAtmosphere, ref remainingToMove, toWorld: false, PressurekPa.Zero, maxTakeRatio);
		int num = Mathf.Min(gridMixingDepth, 6);
		for (int i = 0; i < num; i++)
		{
			CalculateNextShell();
			VentToNextShell(internalAtmosphere, ref remainingToMove, toWorld: false, PressurekPa.Zero, maxTakeRatio);
			if (remainingToMove < DISCARD_REMAINING_THRESHOLD)
			{
				break;
			}
		}
	}

	public static PressurekPa OutwardsPressureRequired(Atmosphere localAtmosphere, PressurekPa maxDelta, int gridMixingDepth, PressurekPa externalTarget)
	{
		if (maxDelta.Equals(PressurekPa.Zero))
		{
			return maxDelta;
		}
		NextShell.Clear();
		NextShell.Add(localAtmosphere);
		int num = Mathf.Min(gridMixingDepth, 6);
		PressurekPa pressurekPa = externalTarget - localAtmosphere.PressureGasses;
		for (int i = 0; i < num; i++)
		{
			CalculateNextShell();
			pressurekPa += PressureDeltaOfShell(externalTarget);
			if (pressurekPa > maxDelta)
			{
				return maxDelta;
			}
		}
		return RocketMath.Max(PressurekPa.Zero, pressurekPa);
	}

	public static PressurekPa InwardsPressureRequired(Atmosphere localAtmosphere, PressurekPa maxDelta, int gridMixingDepth, PressurekPa externalTarget)
	{
		if (maxDelta.Equals(PressurekPa.Zero))
		{
			return maxDelta;
		}
		NextShell.Clear();
		NextShell.Add(localAtmosphere);
		int num = Mathf.Min(gridMixingDepth, 6);
		PressurekPa val = localAtmosphere.PressureGasses - externalTarget;
		for (int i = 0; i < num; i++)
		{
			CalculateNextShell();
			val -= PressureDeltaOfShell(externalTarget);
		}
		return RocketMath.Max(PressurekPa.Zero, val);
	}

	public static PressurekPa PressureDeltaOfShell(PressurekPa externalTarget)
	{
		PressurekPa zero = PressurekPa.Zero;
		foreach (Atmosphere item in NextShell)
		{
			zero += externalTarget - item.PressureGasses;
		}
		return zero;
	}

	private static void CalculateNextShell()
	{
		PreviousShell.Clear();
		PreviousShell.UnionWith(CurrentShell);
		CurrentShell.Clear();
		CurrentShell.UnionWith(NextShell);
		NextShell.Clear();
		foreach (Atmosphere item in CurrentShell)
		{
			for (int i = 0; i < item.OpenNeighbors.Count; i++)
			{
				Atmosphere neighbourAtGrid = GetNeighbourAtGrid(item.OpenNeighbors[i]);
				if (neighbourAtGrid != null && !PreviousShell.Contains(neighbourAtGrid) && !CurrentShell.Contains(neighbourAtGrid))
				{
					NextShell.Add(neighbourAtGrid);
				}
			}
		}
	}

	public static void VentToNextShell(Atmosphere targetAtmosphere, ref PressurekPa remainingToMove, bool toWorld, PressurekPa externalTarget, float maxTakeRatio = 1f)
	{
		PressurekPa zero = PressurekPa.Zero;
		foreach (Atmosphere item in NextShell)
		{
			zero += item.PressureGasses;
		}
		if (zero <= PressurekPa.Zero)
		{
			return;
		}
		foreach (Atmosphere item2 in NextShell)
		{
			TemperatureKelvin totalTemperature = new TemperatureKelvin((targetAtmosphere.Temperature.ToDouble() * targetAtmosphere.TotalMoles.ToDouble() + item2.Temperature.ToDouble() * item2.TotalMoles.ToDouble()) / (item2.TotalMoles.ToDouble() + targetAtmosphere.TotalMoles.ToDouble()));
			PressurekPa val = item2.PressureGasses / zero * maxTakeRatio * remainingToMove;
			val = RocketMath.Min(val, remainingToMove);
			remainingToMove -= (toWorld ? PumpGasToWorld(item2, targetAtmosphere, totalTemperature, val, externalTarget) : PumpGasToPipe(item2, targetAtmosphere, val, externalTarget));
			if (remainingToMove <= PressurekPa.Zero)
			{
				break;
			}
		}
	}

	public static Atmosphere GetNeighbourAtGrid(Grid3 neighbourGrid)
	{
		WorldGrid worldGrid = new WorldGrid(neighbourGrid);
		return AtmosphericsController.World.GetAtmosphereLocal(worldGrid);
	}

	public static PressurekPa PumpGasToWorld(Atmosphere worldAtmosphere, Atmosphere targetAtmosphere, TemperatureKelvin totalTemperature, PressurekPa pressureToMove, PressurekPa externalTarget, bool force = false)
	{
		if (worldAtmosphere == null || targetAtmosphere == null)
		{
			return PressurekPa.Zero;
		}
		if (!force)
		{
			pressureToMove = RocketMath.Min(pressureToMove, externalTarget - worldAtmosphere.PressureGassesAndLiquids);
		}
		MoleQuantity transferMoles = IdealGas.Quantity(pressureToMove, worldAtmosphere.Volume, totalTemperature);
		PressurekPa pressureGasses = worldAtmosphere.PressureGasses;
		worldAtmosphere.Add(targetAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.All));
		return worldAtmosphere.PressureGasses - pressureGasses;
	}

	public static PressurekPa PumpGasToPipe(Atmosphere worldAtmosphere, Atmosphere targetAtmosphere, PressurekPa pressureToMove, PressurekPa externalTarget, bool force = false)
	{
		if (worldAtmosphere == null || targetAtmosphere == null)
		{
			return PressurekPa.Zero;
		}
		if (!force)
		{
			pressureToMove = RocketMath.Min(pressureToMove, worldAtmosphere.PressureGasses - externalTarget);
		}
		MoleQuantity transferMoles = IdealGas.Quantity(pressureToMove, worldAtmosphere.Volume, worldAtmosphere.Temperature);
		targetAtmosphere.Add(worldAtmosphere.Remove(transferMoles, AtmosphereHelper.MatterState.Gas));
		return pressureToMove;
	}
}
