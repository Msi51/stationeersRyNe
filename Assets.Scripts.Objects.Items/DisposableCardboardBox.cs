using System.Text;
using Assets.Scripts.Localization2;

namespace Assets.Scripts.Objects.Items;

public class DisposableCardboardBox : CardboardBox
{
	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Button1)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0.5f,
				ActionMessage = GameStrings.Unpack
			};
			if (!GameManager.RunSimulation || !doAction)
			{
				return delayedActionInstance.Succeed();
			}
			foreach (Slot slot in Slots)
			{
				if (!slot.IsEmpty())
				{
					if (interaction.SourceSlot != null && interaction.SourceSlot.IsEmpty())
					{
						DelayedActionInstance result = HandleSwitch(interaction, slot.SlotIndex, delayedActionInstance, doAction, force: true);
						DestroyIfEmpty();
						return result;
					}
					slot.Get()?.MoveToWorld();
					DestroyIfEmpty();
					return delayedActionInstance.Succeed();
				}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f)
	{
		string contextualName = base.InteractButton1.ContextualName;
		DelayedActionInstance result = new DelayedActionInstance
		{
			Duration = 0.5f,
			ActionMessage = contextualName
		};
		if (!doAction || actionCompletedRatio < 1f)
		{
			return result;
		}
		foreach (Slot slot in Slots)
		{
			if (!slot.IsEmpty())
			{
				OnServer.MoveToWorld(slot.Get(), 0.5f);
				DestroyIfEmpty();
				return result;
			}
		}
		return result;
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button1)
		{
			return GameStrings.Unpack;
		}
		return base.GetContextualName(interactable);
	}

	public void DestroyIfEmpty()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		bool flag = true;
		foreach (Slot slot in Slots)
		{
			if (slot.IsNotEmpty())
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			OnServer.Destroy(this);
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		foreach (Slot slot in Slots)
		{
			if (!slot.IsEmpty())
			{
				extendedText.AppendLine(slot.Get<DynamicThing>().ToTooltip());
			}
		}
		return extendedText;
	}
}
