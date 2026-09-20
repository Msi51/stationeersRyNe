using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects.Pipes;
using Networks;

namespace Objects.Rockets;

public class CrewModulePassiveVent : GasPipeVent, IRocketInternals, IRocketComponent, IWorkingAtmosphere
{
	public new RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public new bool StrictlyInternal => true;

	public new RocketNetwork RocketNetwork { get; set; }

	public new void OnLaunch(bool immediate = false)
	{
	}

	public new void OnLanded(bool immediate = false)
	{
	}

	public new Atmosphere GetWorkingAtmosphere()
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

	public new void CacheWorkingAtmosphere()
	{
		_crewModule = (Cell.IsInCrewModule(base.WorldGrid, out var crewModule) ? crewModule : null);
	}
}
