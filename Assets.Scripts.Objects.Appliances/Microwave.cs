using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Reagents;
using Trading;

namespace Assets.Scripts.Objects.Appliances;

public class Microwave : ApplianceReagentImportBase, IResourceConsumer, IReferencable, IEvaluable
{
	public delegate void OnIngredantAdded(Thing Item);

	public static ItemRecipeComparable RecipeComparable = new ItemRecipeComparable("Microwave");

	protected override Dictionary<Recipe, Item> Recipes => RecipeComparable.Recipes;

	public event OnIngredantAdded OnIngredantAddedEvent;

	public bool CanProcess(Recipe recipe)
	{
		return MicrowaveIngredients.CanProcess(recipe);
	}

	public bool CanProcess(Reagent reagentType)
	{
		return MicrowaveIngredients.CanProcess(reagentType);
	}

	public List<Item> GetResourcesUsed()
	{
		return MicrowaveIngredients.GetResourcesUsed();
	}

	public override void BenchPowerStateChanged(bool receivingPower)
	{
		if (GameManager.RunSimulation)
		{
			OnServer.Interact(this, InteractableType.Powered, (receivingPower && OnOff) ? 1 : 0);
		}
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild.ParentSlot == base.OutputSlot)
		{
			newChild.ScaleToInteractable(base.InteractActivate, 0.8f);
		}
		IsOperable();
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if (newChild.ParentSlot == base.OutputSlot)
		{
			newChild.SetOnBaseOfSlot();
		}
		else
		{
			base.SetSlotOccupantTransformData(newChild);
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		if (OnOff && !IsOpen)
		{
			string arg = (ResultPrefab ? ResultPrefab.ToTooltip() : "???");
			delayedActionInstance.AppendStateMessage(GameStrings.ThingCreateThing, arg, StringManager.Get(base.Completed * 100f));
			if (!Powered)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoPower);
			}
		}
		switch (interactable.Action)
		{
		case InteractableType.OnOff:
			if (IsOpen)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceNotClosed.AsString(ToTooltip()));
				if (doAction)
				{
					OnServer.Interact(interactable, (!OnOff) ? 1 : 0);
				}
				return delayedActionInstance.Succeed();
			}
			break;
		case InteractableType.Open:
			if (!CanCloseAppliance())
			{
				return delayedActionInstance.Fail(GameStrings.MicrowaveOpenFailure);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (!IsOpen) ? 1 : 0);
			return delayedActionInstance.Succeed();
		case InteractableType.Activate:
		{
			if (!IsOpen)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOpen);
			}
			DynamicThing occupant = interaction.SourceSlot.Occupant;
			DynamicThing occupant2 = base.OutputSlot.Occupant;
			Achievements.AssessMuffinMan(base.OutputSlot?.Get());
			Slot outputSlot = base.OutputSlot;
			if (outputSlot != null && outputSlot.Contains<Burger>())
			{
				Achievements.Achieve(Achievements.Kind.AchievementFinallyRealFood);
			}
			if ((bool)occupant2)
			{
				delayedActionInstance.ActionMessage = ActionStrings.Pickup + " " + occupant2.DisplayName;
				delayedActionInstance.Duration = 0f;
				return HandleSwitch(interaction, base.OutputSlot.SlotIndex, delayedActionInstance, doAction);
			}
			string text = ReagentMixture.ToString();
			if (!string.IsNullOrEmpty(text))
			{
				delayedActionInstance.AppendStateMessage(GameStrings.RegentMixString, text);
			}
			GetRecipe();
			if ((bool)ResultPrefab)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceWillProduce, ResultPrefab.ToTooltip());
			}
			if ((bool)occupant)
			{
				IMicrowaveIngredient microwaveIngredient = occupant as IMicrowaveIngredient;
				delayedActionInstance.ActionMessage = ActionStrings.Add + " " + occupant.DisplayName;
				delayedActionInstance.Duration = 0f;
				if (microwaveIngredient == null)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceCanNotAdd);
					return delayedActionInstance.Fail();
				}
				if (microwaveIngredient.AddMixture.TotalReagents <= 0.0)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoUseAbleIngredients);
					return delayedActionInstance.Fail();
				}
				float num = microwaveIngredient.QuantityPerUse;
				IQuantity quantity = occupant as IQuantity;
				if (interaction.AltKey)
				{
					num = 1f;
				}
				if (quantity != null && quantity.GetQuantity < num)
				{
					num = quantity.GetQuantity;
				}
				ReagentMixture reagentMixture = ((num != microwaveIngredient.QuantityPerUse) ? (new ReagentMixture(microwaveIngredient.AddMixture) * (num / microwaveIngredient.QuantityPerUse)) : microwaveIngredient.AddMixture);
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.MicrowaveAddUnitsOf, reagentMixture.TotalReagents.ToStringRounded(), microwaveIngredient.ToTooltip());
					return delayedActionInstance.Succeed();
				}
				if (GameManager.RunSimulation)
				{
					ReagentMixture.Add(reagentMixture);
					this.OnIngredantAddedEvent?.Invoke(occupant);
					microwaveIngredient.OnUseItem(num, this);
				}
				if (interaction.SourceThing.RootParentHuman.IsLocalPlayer)
				{
					UIAudioManager.Play(UIAudioManager.ObjectPutHash);
				}
				return delayedActionInstance.Succeed();
			}
			delayedActionInstance.ActionMessage = ActionStrings.Clear;
			delayedActionInstance.Duration = 2f;
			if (ReagentMixture.TotalReagents <= 0.0)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceNothingToClear);
				return delayedActionInstance.Fail();
			}
			if (!doAction)
			{
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceEmpty);
				return delayedActionInstance.Succeed();
			}
			ReagentMixture.Clear();
			return delayedActionInstance.Succeed();
		}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
