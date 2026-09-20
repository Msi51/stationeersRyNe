using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Pipes;

public class DeviceMixAtmosphere : SmallDevice
{
	[SerializeField]
	[FormerlySerializedAs("Volume")]
	private float volume;

	public virtual VolumeLitres Volume => new VolumeLitres(volume);

	public override bool HasReadableAtmosphere => true;

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState != GameState.None && !(Volume <= VolumeLitres.Zero))
		{
			AtmosphericsManager.Instance.Register(this);
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None && !(Volume <= VolumeLitres.Zero))
		{
			AtmosphericsManager.Instance.Deregister(this);
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (base.InternalAtmosphere == null)
		{
			return;
		}
		foreach (PipeNetwork connectedPipeNetwork in ConnectedPipeNetworks)
		{
			if (connectedPipeNetwork?.Atmosphere != null)
			{
				AtmosphereHelper.Mix(base.InternalAtmosphere, connectedPipeNetwork.Atmosphere, AtmosphereHelper.MatterState.All);
			}
		}
	}
}
