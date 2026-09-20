using Assets.Scripts.Atmospherics;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class LiquidTap : DeviceInputOutput
{
	[SerializeField]
	private PipeValveAnimComponent _pipeValveAnimComponent;

	private Atmosphere InputAtmosphere
	{
		get
		{
			if (ConnectedPipeNetworks.Count <= 0 || ConnectedPipeNetworks[0]?.Atmosphere == null)
			{
				return null;
			}
			return ConnectedPipeNetworks[0].Atmosphere;
		}
	}

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
		if (OnOff)
		{
			Atmosphere toAtmos = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			VolumeLitres maxVolumeToMove = InputAtmosphere.TotalVolumeLiquids / 2.0;
			AtmosphereHelper.DrainLiquids(InputAtmosphere, toAtmos, maxVolumeToMove);
		}
	}
}
