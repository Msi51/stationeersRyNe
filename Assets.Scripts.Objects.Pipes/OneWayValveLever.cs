using Assets.Scripts.Atmospherics;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class OneWayValveLever : DeviceInputOutput, IRocketInternals, IRocketComponent
{
	[SerializeField]
	private PipeValveAnimComponent _pipeValveAnimComponent;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (_pipeValveAnimComponent != null)
		{
			_pipeValveAnimComponent.RefreshState(skipAnimation: true);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (_pipeValveAnimComponent != null)
		{
			_pipeValveAnimComponent.RefreshState(skipAnimation);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (IsOperable && OnOff)
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
