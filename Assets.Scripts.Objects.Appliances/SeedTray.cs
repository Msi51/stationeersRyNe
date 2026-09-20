using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Appliances;

public class SeedTray : Appliance
{
	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		return SlotInteraction(interactable.Slot, interactable, interaction, doAction);
	}

	private DelayedActionInstance SlotInteraction(Slot slot, Interactable interactable, Interaction interaction, bool doAction)
	{
		if (!(interaction.SourceSlot.Occupant is Plant plant))
		{
			return base.InteractWith(interactable, interaction, doAction);
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = slot.DisplayName
		};
		if (slot.Occupant != null)
		{
			if (slot.Occupant.PrefabHash != plant.PrefabHash)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.SlotFull);
				return delayedActionInstance.Fail();
			}
			return base.InteractWith(interactable, interaction, doAction);
		}
		if (doAction)
		{
			plant.SplitStackIntoEmptySlot(slot, 1);
			return delayedActionInstance.Succeed();
		}
		delayedActionInstance.AppendStateMessage(GameStrings.SplicerAddOneToSlot, plant.DisplayName);
		return delayedActionInstance.Succeed();
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if ((object)newChild != null)
		{
			newChild.ScaleToSlot();
			newChild.ThingTransformLocalPosition = Vector3.zero;
			newChild.ThingTransformLocalRotation = Quaternion.identity;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		base.SerializeSave();
		ThingSaveData savedData = new SeedTraySaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}
}
