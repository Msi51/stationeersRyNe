using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class FloorDrain : Pipe
{
	[SerializeField]
	private double WorldlitresPerTick = Chemistry.LiquidPipeVolume.ToDouble();

	private const int MAX_MIXING_ATMOSPHERES = 128;

	private static readonly HashSet<Atmosphere> NextShell = new HashSet<Atmosphere>(128);

	private static readonly HashSet<Atmosphere> CurrentShell = new HashSet<Atmosphere>(128);

	private static readonly HashSet<Atmosphere> PreviousShell = new HashSet<Atmosphere>(128);

	private static readonly VolumeLitres DISCARD_REMAINING_THRESHOLD = new VolumeLitres(0.10000000149011612);

	[SerializeField]
	private int gridMixingDepth = 6;

	private const int MAX_DEPTH = 6;

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		AtmosphericsManager.Instance.Register(this);
	}

	public override void OnAtmosphericTick()
	{
		if (base.HasOpenGrid)
		{
			Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			VolumeLitres remainingToMove = new VolumeLitres(WorldlitresPerTick);
			DrainFlatNeighborsToPipe(atmosphere, ref remainingToMove);
			AtmosphereHelper.Mix(atmosphere, base.PipeNetwork.Atmosphere, AtmosphereHelper.MatterState.Gas);
		}
	}

	private void DrainFlatNeighborsToPipe(Atmosphere localAtmosphere, ref VolumeLitres remainingToMove)
	{
		NextShell.Clear();
		NextShell.Add(localAtmosphere);
		DrainFromNextShell(ref remainingToMove);
		int num = Mathf.Min(gridMixingDepth, 6);
		for (int i = 0; i < num; i++)
		{
			CalculateNextShell();
			DrainFromNextShell(ref remainingToMove);
			if (remainingToMove < DISCARD_REMAINING_THRESHOLD)
			{
				break;
			}
		}
	}

	private void CalculateNextShell()
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
				Grid3 neighbourGrid = item.OpenNeighbors[i];
				if (neighbourGrid.y == base.WorldGrid.Value.y)
				{
					Atmosphere neighbourAtGrid = GetNeighbourAtGrid(neighbourGrid);
					if (neighbourAtGrid != null && !PreviousShell.Contains(neighbourAtGrid) && !CurrentShell.Contains(neighbourAtGrid) && !NeighbourHasOpenGridBelow(neighbourAtGrid))
					{
						NextShell.Add(neighbourAtGrid);
					}
				}
			}
		}
	}

	private bool NeighbourHasOpenGridBelow(Atmosphere neighbourAtmos)
	{
		for (int i = 0; i < neighbourAtmos.OpenNeighbors.Count; i++)
		{
			if (neighbourAtmos.OpenNeighbors[i].y < neighbourAtmos.WorldGrid.Value.y)
			{
				return true;
			}
		}
		return false;
	}

	private static Atmosphere GetNeighbourAtGrid(Grid3 neighbourGrid)
	{
		WorldGrid worldGrid = new WorldGrid(neighbourGrid);
		return AtmosphericsController.World.GetAtmosphereLocal(worldGrid);
	}

	private void DrainFromNextShell(ref VolumeLitres remainingToMove)
	{
		VolumeLitres zero = VolumeLitres.Zero;
		foreach (Atmosphere item in NextShell)
		{
			zero += item.TotalVolumeLiquids;
		}
		if (zero <= VolumeLitres.Zero)
		{
			return;
		}
		foreach (Atmosphere item2 in NextShell)
		{
			if (AtmosphereHelper.IsSubmerged(base.Position, item2, onlyWhenLiquidRendered: false))
			{
				VolumeLitres val = item2.TotalVolumeLiquids / zero * remainingToMove;
				val = RocketMath.Min(val, remainingToMove);
				AtmosphereHelper.DrainLiquids(item2, base.PipeNetwork.Atmosphere, val);
				remainingToMove -= val;
			}
			if (remainingToMove <= VolumeLitres.Zero)
			{
				break;
			}
		}
	}
}
