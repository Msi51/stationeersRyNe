using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Appliances;

public class BasicPackagingMachine : ApplianceReagentImportBase, IResourceConsumer, IReferencable, IEvaluable
{
	public static ItemRecipeComparable RecipeComparable = new ItemRecipeComparable("BasicPackagingMachine");

	public Slot ImportSlot => Slots[0];

	protected override Dictionary<Recipe, Item> Recipes => RecipeComparable.Recipes;

	public bool CanProcess(Recipe recipe)
	{
		return PackageableIngredients.CanProcess(recipe);
	}

	public bool CanProcess(Reagent reagentType)
	{
		return PackageableIngredients.CanProcess(reagentType);
	}

	public List<Item> GetResourcesUsed()
	{
		return PackageableIngredients.GetResourcesUsed();
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		IsOperable();
	}

	public override void SetSlotOccupantTransformData(DynamicThing newChild)
	{
		if (newChild.ParentSlot == ImportSlot)
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
		if (OnOff && !IsOpen && !Powered)
		{
			delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoPower);
		}
		if (interactable.Action == InteractableType.Open && !IsOpen)
		{
			if (!CanCloseAppliance())
			{
				return delayedActionInstance.Fail(GameStrings.InteractCantClose);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			OnServer.Interact(interactable, (!IsOpen) ? 1 : 0);
			return delayedActionInstance.Succeed();
		}
		if (interactable.Action == InteractableType.Activate)
		{
			if (!IsOpen)
			{
				return delayedActionInstance.Fail(GameStrings.DeviceNotOpen);
			}
			DynamicThing occupant = interaction.SourceSlot.Occupant;
			string text = ReagentMixture.ToString();
			if (text != string.Empty)
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
				IPackageableIngredient packageableIngredient = occupant as IPackageableIngredient;
				delayedActionInstance.ActionMessage = ActionStrings.Add + " " + occupant.DisplayName;
				delayedActionInstance.Duration = 0f;
				if (packageableIngredient == null)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceOnlyCookedItems);
					return delayedActionInstance.Fail();
				}
				if (packageableIngredient.AddMixture.TotalReagents <= 0.0)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoUseAbleIngredients);
					return delayedActionInstance.Fail();
				}
				float num = packageableIngredient.QuantityPerUse;
				IQuantity quantity = occupant as IQuantity;
				if (interaction.AltKey)
				{
					num = 1f;
				}
				if (quantity != null && quantity.GetQuantity < num)
				{
					num = quantity.GetQuantity;
				}
				ReagentMixture addMixture = packageableIngredient.AddMixture;
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceAddReagents, addMixture.TotalReagents.ToStringRounded(), packageableIngredient.ToTooltip());
					return delayedActionInstance.Succeed();
				}
				if (GameManager.RunSimulation)
				{
					ReagentMixture.Add(addMixture);
					packageableIngredient.OnUseItem(1f, this);
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
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip passiveTooltip = base.GetPassiveTooltip(hitCollider);
		if (OnOff && Error == 0 && Powered)
		{
			passiveTooltip.State = GameStrings.ThingCreateThing.AsString(ResultPrefab ? ResultPrefab.ToTooltip() : "Unknown?", (base.Completed * 100f).ToStringRounded());
		}
		return passiveTooltip;
	}
}
