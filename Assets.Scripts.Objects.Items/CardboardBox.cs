using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;

namespace Assets.Scripts.Objects.Items;

public class CardboardBox : ItemRenamable
{
	private const float INTERACT_DELAY = 0.5f;

	public override CanEnterResult CanEnter(Slot destinationSlot)
	{
		CanEnterResult result = CanEnterCarboardBox(destinationSlot);
		if (!result.Result)
		{
			return result;
		}
		return base.CanEnter(destinationSlot);
	}

	protected virtual CanEnterResult CanEnterCarboardBox(Slot destinationSlot)
	{
		if (destinationSlot.Parent is CardboardBox)
		{
			return CanEnterResult.Fail(GameStrings.CantNestBoxes);
		}
		return CanEnterResult.Succeed;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Button1 && interaction.SourceSlot != null)
		{
			if (interaction.SourceSlot.IsNotEmpty())
			{
				foreach (Slot slot in Slots)
				{
					if (!slot.IsNotEmpty())
					{
						DelayedActionInstance result = new DelayedActionInstance
						{
							Duration = 0.5f,
							ActionMessage = ActionStrings.Insert
						};
						return HandleSwitch(interaction, slot.SlotIndex, result, doAction);
					}
				}
			}
			else
			{
				foreach (Slot slot2 in Slots)
				{
					if (!slot2.IsEmpty())
					{
						DelayedActionInstance result2 = new DelayedActionInstance
						{
							Duration = 0.5f,
							ActionMessage = ActionStrings.Take
						};
						return HandleSwitch(interaction, slot2.SlotIndex, result2, doAction);
					}
				}
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
