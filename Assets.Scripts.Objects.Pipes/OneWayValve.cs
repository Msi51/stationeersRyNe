using Assets.Scripts.Atmospherics;
using Networks;
using Objects.Rockets;

namespace Assets.Scripts.Objects.Pipes;

public class OneWayValve : DeviceInputOutput, IRocketInternals, IRocketComponent
{
	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (IsOperable)
		{
			AtmosphereHelper.MoveToEqualize(InputNetwork.Atmosphere, OutputNetwork.Atmosphere, PressurekPa.MaxValue, AtmosphereHelper.MatterState.All);
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}
}
