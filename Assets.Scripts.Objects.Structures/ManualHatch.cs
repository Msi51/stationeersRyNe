using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Structures;

public class ManualHatch : UnPoweredDoor
{
	private const int WELDING_TIME = 5;

	private const int WELDING_QUANTITY = 50;

	public bool IsDamaged => DamageState.Total > 0f;

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (base.IsStructureCompleted && interactable.Action == InteractableType.Open && IsLocked)
		{
			return DelayedActionInstance.Failure(interactable.ContextualName, GameStrings.DoorIsWelded, ToTooltip());
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override DelayedActionInstance AttackWith(Attack attack, bool doAction = true)
	{
		if (!base.IsStructureCompleted || IsDamaged || !(attack.SourceItem is IWelder welder))
		{
			return base.AttackWith(attack, doAction);
		}
		if (IsLocked)
		{
			if (welder.IsEmpty)
			{
				return DelayedActionInstance.Failure(GameStrings.ActionUnweld.DisplayString, GameStrings.WeldingTorchNoFuel);
			}
			if (!welder.IsOperable)
			{
				return DelayedActionInstance.Failure(GameStrings.ActionUnweld.DisplayString, GameStrings.ToolNotCurrentlyOperableForTask, welder.ToTooltip());
			}
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 5f,
				ActionMessage = GameStrings.ActionUnweld.DisplayString,
				ExtendedMessage = GameStrings.UnweldingWillUnLockDoor.AsString(ToTooltip())
			};
			if (!doAction)
			{
				return delayedActionInstance;
			}
			if (GameManager.RunSimulation)
			{
				welder.OnUseItem(50f, this);
				OnServer.Interact(base.InteractLock, 0);
			}
			return delayedActionInstance.Succeed();
		}
		if (welder.IsEmpty)
		{
			return DelayedActionInstance.Failure(GameStrings.ActionWeld.DisplayString, GameStrings.WeldingTorchNoFuel);
		}
		if (!welder.IsOperable)
		{
			return DelayedActionInstance.Failure(GameStrings.ActionUnweld.DisplayString, GameStrings.ToolNotCurrentlyOperableForTask, welder.ToTooltip());
		}
		DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
		{
			Duration = 5f,
			ActionMessage = GameStrings.ActionWeld.DisplayString,
			ExtendedMessage = GameStrings.WeldingWillLockDoor.AsString(ToTooltip())
		};
		if (!doAction)
		{
			return delayedActionInstance2;
		}
		if (GameManager.RunSimulation)
		{
			welder.OnUseItem(50f, this);
			OnServer.Interact(base.InteractLock, 1);
		}
		return delayedActionInstance2.Succeed();
	}
}
