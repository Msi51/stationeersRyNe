using System;
using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Unity.Burst.CompilerServices;

namespace Assets.Scripts;

public readonly struct AtmosphericEventInstance
{
	private enum AtmosphericEventAction : byte
	{
		None,
		Reset,
		AddEnergy,
		RemoveEnergy,
		SetGasMixture,
		AddGasMixture,
		RemoveGasMixture,
		RemoveMoles,
		CreateAndAddWorldAtmos,
		CloneGlobal,
		CloneGlobalAddGas,
		CloneGlobalRemoveEnergy,
		DivideWorldAtmosphere,
		StructureReleaseGrid
	}

	private static readonly HashSet<WorldGrid> AwaitingGrids;

	private readonly AtmosphericEventAction _action;

	private readonly bool _spark;

	private readonly AtmosphereHelper.MatterState _matterState;

	private readonly Atmosphere _atmosphere;

	private readonly MoleEnergy _energy;

	private readonly MoleQuantity _moles;

	private readonly WorldGrid _worldGrid;

	private readonly GasMixture _gasMixture;

	private static Queue<AtmosphericEventInstance> atmosphericEventInstances;

	private AtmosphericEventInstance(AtmosphericEventAction action, Atmosphere atmosphere, GasMixture gasMixture)
	{
		_action = action;
		_atmosphere = atmosphere;
		_gasMixture = gasMixture;
		atmosphere.IsAwaitingEvent = true;
		_spark = false;
		_matterState = AtmosphereHelper.MatterState.All;
		_energy = default(MoleEnergy);
		_moles = default(MoleQuantity);
		_worldGrid = default(WorldGrid);
	}

	private AtmosphericEventInstance(AtmosphericEventAction action, Atmosphere atmosphere)
	{
		_action = action;
		_atmosphere = atmosphere;
		atmosphere.IsAwaitingEvent = true;
		_spark = false;
		_matterState = AtmosphereHelper.MatterState.All;
		_energy = default(MoleEnergy);
		_moles = default(MoleQuantity);
		_worldGrid = default(WorldGrid);
		_gasMixture = default(GasMixture);
	}

	private AtmosphericEventInstance(AtmosphericEventAction action, Atmosphere atmosphere, MoleQuantity moles, AtmosphereHelper.MatterState matterState)
	{
		_action = action;
		_atmosphere = atmosphere;
		_moles = moles;
		_matterState = matterState;
		atmosphere.IsAwaitingEvent = true;
		_spark = false;
		_energy = default(MoleEnergy);
		_worldGrid = default(WorldGrid);
		_gasMixture = default(GasMixture);
	}

	private AtmosphericEventInstance(AtmosphericEventAction action, WorldGrid worldGrid, GasMixture gasMixture, bool spark = false)
	{
		_action = action;
		_worldGrid = worldGrid;
		_gasMixture = gasMixture;
		_spark = false;
		_matterState = AtmosphereHelper.MatterState.All;
		_atmosphere = null;
		_energy = default(MoleEnergy);
		_moles = default(MoleQuantity);
	}

	private AtmosphericEventInstance(AtmosphericEventAction action, WorldGrid worldGrid)
	{
		_action = action;
		_worldGrid = worldGrid;
		_spark = false;
		_matterState = AtmosphereHelper.MatterState.All;
		_atmosphere = null;
		_energy = default(MoleEnergy);
		_moles = default(MoleQuantity);
		_gasMixture = default(GasMixture);
	}

	private AtmosphericEventInstance(AtmosphericEventAction action, WorldGrid worldGrid, MoleEnergy energy, bool spark = false)
	{
		_action = action;
		_worldGrid = worldGrid;
		_energy = energy;
		_spark = spark;
		_matterState = AtmosphereHelper.MatterState.All;
		_atmosphere = null;
		_moles = default(MoleQuantity);
		_gasMixture = default(GasMixture);
	}

	private AtmosphericEventInstance(AtmosphericEventAction action, Atmosphere atmosphere, MoleEnergy energy, bool spark = false)
	{
		_action = action;
		_atmosphere = atmosphere;
		_energy = energy;
		_spark = spark;
		atmosphere.IsAwaitingEvent = true;
		_matterState = AtmosphereHelper.MatterState.All;
		_moles = default(MoleQuantity);
		_worldGrid = default(WorldGrid);
		_gasMixture = default(GasMixture);
	}

	public static void HandleNetworkChangedEvents()
	{
		AtmosphericEventInstance next;
		while (TryDequeue(out next))
		{
			next.Apply();
		}
	}

	public static void Clear()
	{
		lock (atmosphericEventInstances)
		{
			atmosphericEventInstances.Clear();
		}
		ClearAwaiting();
	}

	private static void AddEvent(AtmosphericEventInstance newEvent)
	{
		lock (atmosphericEventInstances)
		{
			atmosphericEventInstances.Enqueue(newEvent);
		}
	}

	public static bool TryDequeue(out AtmosphericEventInstance next)
	{
		lock (atmosphericEventInstances)
		{
			return atmosphericEventInstances.TryDequeue(out next);
		}
	}

	public static void CreateAddEnergy(Atmosphere atmosphere, MoleEnergy energy, bool spark)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.AddEnergy, atmosphere, energy, spark));
		}
	}

	public static void CreateRemoveEnergy(Atmosphere atmosphere, MoleEnergy energy)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.RemoveEnergy, atmosphere, energy));
		}
	}

	public static void CreateSet(Atmosphere atmosphere, GasMixture gasMixture)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.SetGasMixture, atmosphere, gasMixture));
		}
	}

	public static void CreateRemove(Atmosphere atmosphere, GasMixture gasMixture)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.RemoveGasMixture, atmosphere, gasMixture));
		}
	}

	public static void RemoveMoles(Atmosphere atmosphere, MoleQuantity moles, AtmosphereHelper.MatterState matterState)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.RemoveMoles, atmosphere, moles, matterState));
		}
	}

	public static void CreateRemove(Atmosphere atmosphere, MoleMixture moleMixture)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.RemoveGasMixture, atmosphere, moleMixture.ToGasMixture()));
		}
	}

	public static void CreateAdd(Atmosphere atmosphere, GasMixture gasMixture)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.AddGasMixture, atmosphere, gasMixture));
		}
	}

	public static void CreateEmpty(WorldGrid grid)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.CreateAndAddWorldAtmos, grid, GasMixtureHelper.Create()));
		}
	}

	public static void CreateEmptyAndAdd(WorldGrid grid, GasMixture gasMixture)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.CreateAndAddWorldAtmos, grid, gasMixture));
		}
	}

	public static void Reset(Atmosphere atmosphere)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.Reset, atmosphere));
		}
	}

	public static void CloneGlobal(WorldGrid grid, MoleEnergy energy, bool spark = false)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.CloneGlobal, grid, energy, spark));
		}
	}

	public static void CloneGlobalAddGasMix(WorldGrid grid, GasMixture gasMixture, bool spark = false)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.CloneGlobalAddGas, grid, gasMixture, spark));
		}
	}

	public static void CloneGlobalRemoveEnergy(WorldGrid grid, MoleEnergy energyToRemove)
	{
		if (GameManager.RunSimulation)
		{
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.CloneGlobalRemoveEnergy, grid, energyToRemove));
		}
	}

	public static void StructureBlockingGrid(WorldGrid wGrid)
	{
		if (GameManager.RunSimulation)
		{
			Atmosphere atmosphereLocal = AtmosphericsController.World.GetAtmosphereLocal(wGrid);
			if (atmosphereLocal != null)
			{
				atmosphereLocal.IsAwaitingEvent = true;
			}
			AddAwaitingGrid(wGrid);
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.DivideWorldAtmosphere, wGrid));
		}
	}

	public static void StructureReleaseGrid(WorldGrid wGrid)
	{
		if (GameManager.RunSimulation)
		{
			AddAwaitingGrid(wGrid);
			AddEvent(new AtmosphericEventInstance(AtmosphericEventAction.StructureReleaseGrid, wGrid, GasMixtureHelper.Create()));
		}
	}

	public static bool IsGridAwaiting(WorldGrid wGrid)
	{
		lock (AwaitingGrids)
		{
			if (AwaitingGrids.Contains(wGrid))
			{
				return true;
			}
		}
		return false;
	}

	public static void ClearAwaiting()
	{
		lock (AwaitingGrids)
		{
			AwaitingGrids.Clear();
		}
	}

	private static void AddAwaitingGrid(WorldGrid wGrid)
	{
		lock (AwaitingGrids)
		{
			AwaitingGrids.Add(wGrid);
		}
	}

	private void Apply()
	{
		switch (_action)
		{
		case AtmosphericEventAction.Reset:
			Reset();
			break;
		case AtmosphericEventAction.AddEnergy:
			AddEnergy(_atmosphere);
			break;
		case AtmosphericEventAction.RemoveEnergy:
			RemoveEnergy(_atmosphere);
			break;
		case AtmosphericEventAction.SetGasMixture:
			SetGasMixture();
			break;
		case AtmosphericEventAction.AddGasMixture:
			AddGasMixture();
			break;
		case AtmosphericEventAction.RemoveGasMixture:
			RemoveGasMixture();
			break;
		case AtmosphericEventAction.RemoveMoles:
			RemoveMoles();
			break;
		case AtmosphericEventAction.CreateAndAddWorldAtmos:
			CreateAndAddWorldAtmos();
			break;
		case AtmosphericEventAction.CloneGlobal:
			CloneGlobal();
			break;
		case AtmosphericEventAction.CloneGlobalAddGas:
			CloneGlobalAddGas();
			break;
		case AtmosphericEventAction.CloneGlobalRemoveEnergy:
			CloneGlobalRemoveEnergy();
			break;
		case AtmosphericEventAction.DivideWorldAtmosphere:
			DivideWorldAtmosphere();
			break;
		case AtmosphericEventAction.StructureReleaseGrid:
			StructureReleaseGrid();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case AtmosphericEventAction.None:
			break;
		}
	}

	private void AddGasMixture()
	{
		if (_atmosphere != null)
		{
			_atmosphere.Add(_gasMixture);
			_atmosphere.IsAwaitingEvent = false;
		}
	}

	private void CreateAndAddWorldAtmos()
	{
		Atmosphere obj = GridController.World.AtmosphericsController.GetAtmosphereLocal(_worldGrid) ?? new Atmosphere(_worldGrid, 0L);
		obj.Add(_gasMixture);
		obj.IsAwaitingEvent = false;
	}

	[SkipLocalsInit]
	private void StructureReleaseGrid()
	{
		Atmosphere atmosphere = GridController.World.AtmosphericsController.GetAtmosphereLocal(_worldGrid);
		if (atmosphere == null)
		{
			bool flag = true;
			atmosphere = new Atmosphere(_worldGrid, 0L);
			Span<Grid3> bufClosed = stackalloc Grid3[6];
			Span<Grid3> bufOpen = stackalloc Grid3[6];
			atmosphere.GetOpenAirNeighbors(bufClosed, bufOpen);
			int num = 0;
			for (int i = 0; i < atmosphere.OpenNeighbors.Count; i++)
			{
				Atmosphere atmosphereLocal = GridController.World.AtmosphericsController.GetAtmosphereLocal(new WorldGrid(atmosphere.OpenNeighbors[i]));
				if (atmosphereLocal != null)
				{
					num++;
					if (atmosphereLocal.Room != null)
					{
						flag = false;
						break;
					}
				}
			}
			if (num >= atmosphere.OpenNeighbors.Count)
			{
				flag = false;
			}
			if (flag && !PlanetaryAtmosphereSimulation.IsInSpaceAtmosphere(_worldGrid))
			{
				atmosphere.Add(PlanetaryAtmosphereSimulation.TakeGlobalGasMix(atmosphere.Volume));
			}
		}
		atmosphere.IsAwaitingEvent = false;
	}

	private void CloneGlobal()
	{
		Atmosphere atmosphere = GridController.World.AtmosphericsController.CloneGlobalAtmosphere(_worldGrid, 0L);
		if (atmosphere != null)
		{
			AddEnergy(atmosphere);
			atmosphere.IsAwaitingEvent = false;
		}
	}

	private void CloneGlobalAddGas()
	{
		Atmosphere atmosphere = GridController.World.AtmosphericsController.CloneGlobalAtmosphere(_worldGrid, 0L);
		if (atmosphere != null)
		{
			atmosphere.Add(_gasMixture);
			atmosphere.IsAwaitingEvent = false;
		}
	}

	private void Reset()
	{
		if (_atmosphere != null)
		{
			_atmosphere.GasMixture.Reset();
			_atmosphere.IsAwaitingEvent = false;
		}
	}

	private void CloneGlobalRemoveEnergy()
	{
		Atmosphere atmosphere = GridController.World.AtmosphericsController.CloneGlobalAtmosphere(_worldGrid, 0L);
		if (atmosphere != null)
		{
			RemoveEnergy(atmosphere);
			atmosphere.IsAwaitingEvent = false;
		}
	}

	private void DivideWorldAtmosphere()
	{
		Atmosphere atmosphereLocal = AtmosphericsController.World.GetAtmosphereLocal(_worldGrid);
		if (atmosphereLocal == null || atmosphereLocal.BeingDestroyed || !(atmosphereLocal.TotalMoles > MoleQuantity.Zero))
		{
			return;
		}
		lock (atmosphereLocal.OpenNeighbors)
		{
			int count = atmosphereLocal.OpenNeighbors.Count;
			if (count > 0)
			{
				GasMixture gasMixture = new GasMixture(atmosphereLocal.GasMixture);
				gasMixture.Divide(count);
				int index = count;
				while (index-- > 0)
				{
					Grid3 position = atmosphereLocal.OpenNeighbors[index];
					AtmosphericsController.World.CloneGlobalAtmosphere(new WorldGrid(position), 0L).Add(gasMixture);
				}
			}
		}
		AtmosphericsManager.AllAtmospheres.Remove(atmosphereLocal);
		atmosphereLocal.GasMixture.Reset();
	}

	private void RemoveGasMixture()
	{
		if (_atmosphere != null)
		{
			_atmosphere.GasMixture.Remove(_gasMixture);
			_atmosphere.IsAwaitingEvent = false;
		}
	}

	private void RemoveMoles()
	{
		if (_atmosphere != null)
		{
			_atmosphere.GasMixture.Remove(_moles, _matterState);
			_atmosphere.IsAwaitingEvent = false;
		}
	}

	private void SetGasMixture()
	{
		if (_atmosphere != null)
		{
			_atmosphere.GasMixture.Set(_gasMixture);
			_atmosphere.IsAwaitingEvent = false;
		}
	}

	private void AddEnergy(Atmosphere atmosphere)
	{
		if (atmosphere != null)
		{
			atmosphere.GasMixture.AddEnergy(_energy);
			if (_spark)
			{
				atmosphere.Sparked = true;
			}
			atmosphere.IsAwaitingEvent = false;
		}
	}

	private void RemoveEnergy(Atmosphere atmosphere)
	{
		if (atmosphere != null)
		{
			atmosphere.GasMixture.RemoveEnergy(_energy);
			atmosphere.IsAwaitingEvent = false;
		}
	}

	static AtmosphericEventInstance()
	{
		AwaitingGrids = new HashSet<WorldGrid>(32);
		atmosphericEventInstances = new Queue<AtmosphericEventInstance>(1024);
	}
}
