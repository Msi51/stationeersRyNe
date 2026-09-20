using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using Sound;
using UnityEngine;

namespace Objects.Pipes;

public class VolumePump : SettableAtmosDevice, IRocketInternals, IRocketComponent
{
	[SerializeField]
	private float wheelSettingIncrement = 10f;

	[SerializeField]
	private float wheelAltSettingIncrement = 1f;

	private PooledAudioSource _volumePumpRunnning;

	private static readonly int VolumePumpRunningHash = Animator.StringToHash("VolumePumpRunning");

	private static readonly int VolumePumpStartHash = Animator.StringToHash("VolumePumpStart");

	private const float AUDIO_SQUARE_DISTANCE = 100f;

	public virtual float MinOperatingSoundPitch => 1f;

	public virtual float MaxOperatingSoundPitch => 1.2f;

	public override float WheelSettingIncrement => wheelSettingIncrement;

	public override float WheelAltSettingIncrement => wheelAltSettingIncrement;

	protected virtual int VolumePumpRunningSound => VolumePumpRunningHash;

	protected virtual int VolumePumpStartSound => VolumePumpStartHash;

	public override float AudioDistanceSquared => 100f;

	public virtual VolumePumpFlowDirection PumpDirection => VolumePumpFlowDirection.Right;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public override void Awake()
	{
		base.Awake();
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (!OnOff || !Powered || Error == 1 || !IsAudible())
		{
			_volumePumpRunnning?.Stop();
			_volumePumpRunnning = null;
			return;
		}
		if (!_volumePumpRunnning)
		{
			_volumePumpRunnning = PlayPooledAudioSound(VolumePumpRunningSound, Vector3.zero);
		}
		if ((bool)_volumePumpRunnning)
		{
			_volumePumpRunnning.GameAudioSource.SetPitchMultiplier(VolumePumpRunningSound, Mathf.Lerp(MinOperatingSoundPitch, MaxOperatingSoundPitch, base.OutputSetting / MaxSetting));
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (!IsAudible())
		{
			return;
		}
		switch (interactable.Action)
		{
		case InteractableType.OnOff:
			PlayPooledAudioSound(OnOff ? Defines.Sounds.PipeDeviceOn : Defines.Sounds.PipeDeviceOff, Vector3.zero);
			if (VolumePumpStartSound != 0 && OnOff && Powered && Error == 0)
			{
				PlayPooledAudioSound(VolumePumpStartSound, Vector3.zero);
			}
			break;
		case InteractableType.Error:
			if (Error == 1 && interactable.State != Error)
			{
				PlayPooledAudioSound(Defines.Sounds.Error, Vector3.zero);
			}
			break;
		}
	}

	public override void OnDestroy()
	{
		_volumePumpRunnning?.Stop(VolumePumpRunningSound);
		base.OnDestroy();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = HandleButtonSetting(interactable, interaction, doAction);
		if (delayedActionInstance != null)
		{
			return delayedActionInstance;
		}
		DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		delayedActionInstance2.AppendStateMessage(GameStrings.GlobalVolumeLitre, StringManager.Get(base.OutputSetting));
		delayedActionInstance2.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
		delayedActionInstance2.AppendStateMessage(GameStrings.UseLabelerToSet);
		switch (interactable.Action)
		{
		case InteractableType.Button1:
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: true, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting += (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button2:
			if (!doAction)
			{
				return delayedActionInstance2.Succeed();
			}
			SettingWheel.PlayWheelSound(this, SettingWheel.Wheel, increaseSetting: false, interaction.AltKey, base.OutputSetting, MinSetting, MaxSetting, WheelSettingIncrement, WheelAltSettingIncrement);
			if (GameManager.RunSimulation)
			{
				base.OutputSetting -= (interaction.AltKey ? WheelAltSettingIncrement : WheelSettingIncrement);
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (base.PowerCable == null || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return (float)base.Setting / MaxSetting * UsedPower;
	}

	public void FlowInDirection(VolumePumpFlowDirection flowDirection)
	{
		switch (flowDirection)
		{
		case VolumePumpFlowDirection.Right:
			MoveAtmosphere(InputNetwork.Atmosphere, OutputNetwork.Atmosphere);
			break;
		case VolumePumpFlowDirection.Left:
			MoveAtmosphere(OutputNetwork.Atmosphere, InputNetwork.Atmosphere);
			break;
		}
	}

	private void MoveAtmosphere(Atmosphere inputAtmosphere, Atmosphere outputAtmosphere)
	{
		switch (outputAtmosphere.AllowedMatterState)
		{
		case AtmosphereHelper.MatterState.Liquid:
			AtmosphereHelper.MoveLiquidVolume(inputAtmosphere, outputAtmosphere, new VolumeLitres(base.OutputSetting));
			AtmosphereHelper.MoveToEqualize(inputAtmosphere, outputAtmosphere, PressurekPa.MaxValue, AtmosphereHelper.MatterState.Gas);
			break;
		case AtmosphereHelper.MatterState.Gas:
		case AtmosphereHelper.MatterState.All:
			AtmosphereHelper.MoveVolume(inputAtmosphere, outputAtmosphere, new VolumeLitres(base.OutputSetting), AtmosphereHelper.MatterState.All, MoleQuantity.Zero);
			break;
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (OnOff && Powered && Error != 1 && InputNetwork?.Atmosphere != null && OutputNetwork?.Atmosphere != null)
		{
			FlowInDirection(PumpDirection);
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}
}
