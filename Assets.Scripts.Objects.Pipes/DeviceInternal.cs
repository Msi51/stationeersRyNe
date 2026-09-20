using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Pipes;

public class DeviceInternal : DeviceOutput, IThermal, IVolume
{
	[FormerlySerializedAs("Volume")]
	[SerializeField]
	public float volume;

	public Pipe.ContentType ContentType = Pipe.ContentType.Gas;

	public VolumeLitres Volume => new VolumeLitres(volume);

	public virtual AtmosphereHelper.MatterState MatterState => ContentType switch
	{
		Pipe.ContentType.Unknown => AtmosphereHelper.MatterState.All, 
		Pipe.ContentType.Gas => AtmosphereHelper.MatterState.Gas, 
		Pipe.ContentType.Liquid => AtmosphereHelper.MatterState.Liquid, 
		Pipe.ContentType.All => AtmosphereHelper.MatterState.All, 
		_ => AtmosphereHelper.MatterState.All, 
	};

	public override bool HasReadableAtmosphere => true;

	public VolumeLitres GetVolume => Volume;

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
		if (base.InternalAtmosphere != null && base.IsOutputValid)
		{
			AtmosphereHelper.Mix(base.InternalAtmosphere, ConnectedPipeNetwork.Atmosphere, MatterState);
		}
	}
}
