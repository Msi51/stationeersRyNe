using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Electrical;

public class LandingPadGasStorage : LandingPadModular
{
	[SerializeField]
	private float _volume = 500f;

	private int _damageTicker;

	public override VolumeLitres Volume => new VolumeLitres(_volume);

	public override bool Stressed
	{
		get
		{
			return _stressed;
		}
		set
		{
			if (value != _stressed)
			{
				if (value)
				{
					AtmosphericAudioHandler.Instance.AddStressedPipe(this);
				}
				else
				{
					AtmosphericAudioHandler.Instance.RemoveStressedPipe(this);
				}
				_stressed = value;
			}
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		AtmosphericsManager.Instance.Register(this);
	}

	public override void OnDeregistered()
	{
		base.OnDeregistered();
		AtmosphericsManager.Instance.Deregister(this);
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!base.HasOpenGrid)
		{
			return;
		}
		Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		PressurekPa pressurekPa = PressurekPa.Zero;
		if (base.LandingPadNetwork?.Atmosphere == null)
		{
			return;
		}
		PressurekPa pressureGassesAndLiquids = base.LandingPadNetwork.Atmosphere.PressureGassesAndLiquids;
		if (atmosphere != null)
		{
			pressurekPa = atmosphere.PressureGassesAndLiquids;
		}
		PressurekPa pressurekPa2 = RocketMath.Abs(pressurekPa - pressureGassesAndLiquids);
		if (IsBroken)
		{
			if (pressureGassesAndLiquids < Chemistry.ResetThreshold && atmosphere == null)
			{
				base.LandingPadNetwork.Atmosphere.GasMixture.Reset();
				return;
			}
			Atmosphere outputAtmos = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			AtmosphereHelper.Mix(base.LandingPadNetwork.Atmosphere, outputAtmos, AtmosphereHelper.MatterState.All);
		}
		else if (pressurekPa2 >= base.LandingPadNetwork.MaxPressureKpa)
		{
			if (_damageTicker > 5)
			{
				DamageState.Damage(ChangeDamageType.Increment, 5f, DamageUpdateType.Brute);
			}
			_damageTicker++;
		}
		else
		{
			_damageTicker = 0;
		}
	}

	public override void OnStructureBroken()
	{
		base.OnStructureBroken();
		if (GameManager.GameState == GameState.Running && !GameManager.IsBatchMode)
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(Defines.Sounds.PipeFailHash, base.Position + Vector3.up);
		}
	}
}
