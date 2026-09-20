using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class Valve : DeviceAtmospherics, IRocketInternals, IRocketComponent
{
	[SerializeField]
	private PipeValveAnimComponent pipeValveAnimComponent;

	public PipeValveAnimComponent PipeValveAnimComponent => pipeValveAnimComponent;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	protected override bool IsOperable
	{
		get
		{
			if (Error == 1)
			{
				if (!HasPipeNetwork)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (HasPipeNetwork)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

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

	private Atmosphere OutputAtmosphere
	{
		get
		{
			if (ConnectedPipeNetworks.Count <= 1 || ConnectedPipeNetworks[1]?.Atmosphere == null)
			{
				return null;
			}
			return ConnectedPipeNetworks[1].Atmosphere;
		}
	}

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
		if (!OnOff || Error == 1 || (HasPowerState && !Powered))
		{
			return;
		}
		foreach (PipeNetwork connectedPipeNetwork in ConnectedPipeNetworks)
		{
			if (connectedPipeNetwork.IsAwaitingEvent)
			{
				return;
			}
		}
		if (InputAtmosphere != null && OutputAtmosphere != null)
		{
			AtmosphereHelper.Mix(InputAtmosphere, OutputAtmosphere, AtmosphereHelper.MatterState.All);
		}
		foreach (PipeNetwork connectedPipeNetwork2 in ConnectedPipeNetworks)
		{
			if (connectedPipeNetwork2.Atmosphere.PressureGassesAndLiquids < Chemistry.ResetThreshold)
			{
				connectedPipeNetwork2.Atmosphere.GasMixture.Reset();
				break;
			}
		}
	}
}
