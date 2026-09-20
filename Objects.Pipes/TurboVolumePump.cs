using Assets.Scripts.Localization2;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Effects;
using UnityEngine;

namespace Objects.Pipes;

public class TurboVolumePump : VolumePump
{
	protected VolumePumpFlowDirection FlowDirection;

	[SerializeField]
	private MaterialChanger directionControl0;

	[SerializeField]
	private MaterialChanger directionControl1;

	private static readonly int TurboVolumePumpRunningHash = Animator.StringToHash("TurboVolumePumpRunning");

	private const float AUDIO_SQUARE_DISTANCE = 225f;

	public static readonly int ButtonHash = Animator.StringToHash("Button");

	public static readonly float BasePowerDraw = 200f;

	private static readonly string[] FlowDirectionModes = new string[2] { "Right", "Left" };

	protected override int VolumePumpRunningSound => TurboVolumePumpRunningHash;

	protected override int VolumePumpStartSound => 0;

	public override float AudioDistanceSquared => 225f;

	public override float MinOperatingSoundPitch => 0.75f;

	public override float MaxOperatingSoundPitch => 3f;

	public override string[] ModeStrings => FlowDirectionModes;

	public override VolumePumpFlowDirection PumpDirection => (VolumePumpFlowDirection)Mode;

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		delayedActionInstance.AppendStateMessage(GameStrings.VolumeChangeDirection);
		switch (interactable.Action)
		{
		case InteractableType.Button3:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			FlowDirection = VolumePumpFlowDirection.Right;
			OnServer.Interact(base.InteractMode, 0);
			PlaySound(ButtonHash);
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button4:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			FlowDirection = VolumePumpFlowDirection.Left;
			OnServer.Interact(base.InteractMode, 1);
			PlaySound(ButtonHash);
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (IsAudible() && interactable.Action == InteractableType.Mode)
		{
			PlayPooledAudioSound((Mode == 1) ? Defines.Sounds.PipeDeviceOn : Defines.Sounds.PipeDeviceOff, Vector3.zero);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.SwitchOnOff.RefreshState(skipAnimation);
		directionControl0.ChangeState((Mode != 0) ? ((OnOff && Powered) ? Defines.Animator.OffPowered : Defines.Animator.Off) : ((OnOff && Powered) ? Defines.Animator.OnPowered : Defines.Animator.On));
		directionControl1.ChangeState((Mode != 1) ? ((OnOff && Powered) ? Defines.Animator.OffPowered : Defines.Animator.Off) : ((OnOff && Powered) ? Defines.Animator.OnPowered : Defines.Animator.On));
	}

	public override float GetUsedPower(CableNetwork cableNetwork)
	{
		if (!base.PowerCable || base.PowerCable.CableNetwork != cableNetwork)
		{
			return -1f;
		}
		if (!OnOff)
		{
			return 0f;
		}
		return BasePowerDraw + (float)base.Setting / MaxSetting * UsedPower;
	}
}
