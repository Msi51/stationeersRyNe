using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Networks;

namespace Objects.Rockets;

public class CrewModuleActiveVent : ActiveVent, IRocketInternals, IRocketComponent, IWorkingAtmosphere
{
	private CrewModule _crewModule;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public override CanMountResult CanMountOnWall()
	{
		return CanMountResult.BasicValid;
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override Atmosphere GetWorkingAtmosphere()
	{
		if (_crewModule != null)
		{
			return _crewModule.InternalAtmosphere;
		}
		return base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		_crewModule = (Cell.IsInCrewModule(base.WorldGrid, out var crewModule) ? crewModule : null);
	}

	public void CacheWorkingAtmosphere()
	{
		_crewModule = (Cell.IsInCrewModule(base.WorldGrid, out var crewModule) ? crewModule : null);
	}
}
