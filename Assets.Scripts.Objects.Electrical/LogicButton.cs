using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Motherboards;
using Cysharp.Threading.Tasks;

namespace Assets.Scripts.Objects.Electrical;

public class LogicButton : LogicInputBase
{
	private bool _activated;

	public event Event ButtonPress;

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return Setting;
		}
		return base.GetLogicValue(logicType);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (!_activated && GameManager.RunSimulation && interactable.Action == InteractableType.Activate && interactable.State == 1)
		{
			Setting = interactable.State;
			WaitThenStop().Forget();
		}
	}

	private async UniTaskVoid WaitThenStop()
	{
		await UniTask.Delay(550, ignoreTimeScale: false, PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
		Setting = 0.0;
		_activated = false;
		OnServer.Interact(base.InteractActivate, 0);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (IsLocked)
		{
			return delayedActionInstance.Fail(GameStrings.DeviceLocked.AsColor("red"));
		}
		if (interactable.Action == InteractableType.Activate)
		{
			if (Activate == 1)
			{
				return delayedActionInstance.Fail(GameStrings.GlobalAlreadyInUse);
			}
			if (!IsAuthorized(interaction.SourceThing))
			{
				return delayedActionInstance.Fail(GameStrings.AccessCardUnableToInteract);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, 1);
			this.ButtonPress?.Invoke();
			return delayedActionInstance.Succeed();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
