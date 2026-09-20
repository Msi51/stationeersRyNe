using Networks;
using Objects.Rockets;

namespace Assets.Scripts.Objects.Structures;

public class CrewModuleWallLight : WallLight, IRocketInternals, IRocketComponent
{
	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => true;

	public RocketNetwork RocketNetwork { get; set; }

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override CanMountResult CanMountOnWall()
	{
		return CanMountResult.BasicValid;
	}
}
