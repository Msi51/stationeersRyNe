using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Util;
using Networks;
using Objects.Rockets;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class VolumeRegulator : SettableAtmosDevice, IRocketInternals, IRocketComponent
{
	[Header("Pressure Regulator")]
	public RegulatorType RegulatorType;

	public static readonly VolumeLitres BaseVolumePerTick = new VolumeLitres(0.25);

	public override float WheelSettingIncrement => 10f;

	public override float WheelAltSettingIncrement => 1f;

	public RocketInternalCellType InternalCellType => RocketInternalCellType.Devices;

	public bool StrictlyInternal => false;

	public RocketNetwork RocketNetwork { get; set; }

	public override void Awake()
	{
		base.Awake();
		MaxSetting = 100f;
		if (SettingWheel != null)
		{
			SettingWheel.WheelSpinSpeed = 1.5f;
		}
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
		switch (RegulatorType)
		{
		case RegulatorType.Upstream:
			delayedActionInstance2.AppendStateMessage(GameStrings.OutputVolumeRatio, StringManager.Get((int)base.OutputSetting));
			break;
		case RegulatorType.Downstream:
			delayedActionInstance2.AppendStateMessage(GameStrings.InputVolumeRatio, StringManager.Get((int)base.OutputSetting));
			break;
		}
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

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (OnOff && Powered && Error != 1 && IsOperable && AtmosphereHelper.MoveRegulatedLiquidVolume(InputNetwork.Atmosphere, OutputNetwork.Atmosphere, BaseVolumePerTick, base.OutputSetting, RegulatorType))
		{
			AtmosphereHelper.MoveToEqualize(InputNetwork.Atmosphere, OutputNetwork.Atmosphere, PressurekPa.MaxValue, AtmosphereHelper.MatterState.Gas);
		}
	}

	public void OnLaunch(bool immediate = false)
	{
	}

	public void OnLanded(bool immediate = false)
	{
	}
}
