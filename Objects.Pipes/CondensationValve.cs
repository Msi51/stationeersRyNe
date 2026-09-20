using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Objects.Pipes;

public class CondensationValve : DeviceInputOutput, IRocketInternals, IRocketComponent
{
	[SerializeField]
	private PipeValveAnimComponent pipeValveAnimComponent;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public PipeValveAnimComponent PipeValveAnimComponent => pipeValveAnimComponent;

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if (PipeValveAnimComponent != null)
		{
			PipeValveAnimComponent.RefreshState(skipAnimation: true);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
		if (PipeValveAnimComponent != null)
		{
			PipeValveAnimComponent.RefreshState(skipAnimation);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (OnOff && InputNetwork != null && OutputNetwork != null)
		{
			VolumeLitres maxVolumeToMove = RocketMath.Min(Chemistry.PipeVolume, InputNetwork.Atmosphere.TotalVolumeLiquids / 2.0);
			AtmosphereHelper.DrainLiquids(InputNetwork.Atmosphere, OutputNetwork.Atmosphere, maxVolumeToMove);
		}
	}
}
