using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Reagents;
using Trading;

namespace Assets.Scripts.Objects.Appliances;

public class ChemistryStation : ApplianceReagentImportBase, IResourceConsumer, IReferencable, IEvaluable
{
	public static List<IChemistryIngredient> AllIngredients = new List<IChemistryIngredient>();

	public static ItemRecipeComparable RecipeComparable = new ItemRecipeComparable("ChemistryStation");

	protected override Dictionary<Recipe, Item> Recipes => RecipeComparable.Recipes;

	protected override bool IsError
	{
		get
		{
			if (!OnOff || !Powered || IsOpen || (bool)base.OutputSlot.Occupant || ReagentMixture.TotalReagents <= 0.0)
			{
				return true;
			}
			return false;
		}
	}

	public bool CanProcess(Recipe recipe)
	{
		foreach (IChemistryIngredient allIngredient in AllIngredients)
		{
			if (allIngredient is Item item && item.CreatedReagentMixture.ContainsSome(recipe))
			{
				return true;
			}
		}
		return false;
	}

	public bool CanProcess(Reagent reagentType)
	{
		foreach (IChemistryIngredient allIngredient in AllIngredients)
		{
			if (allIngredient is Item item && item.CreatedReagentMixture.Contains(reagentType))
			{
				return true;
			}
		}
		return false;
	}

	public List<Item> GetResourcesUsed()
	{
		List<Item> list = new List<Item>(AllIngredients.Count);
		foreach (IChemistryIngredient allIngredient in AllIngredients)
		{
			Item item = Prefab.Find(allIngredient.GetPrefabHash()) as Item;
			if (item == null || list.Contains(item))
			{
				continue;
			}
			foreach (KeyValuePair<Recipe, Item> allRecipe in RecipeComparable.AllRecipes)
			{
				if (item.CreatedReagentMixture.ContainsSome(allRecipe.Key))
				{
					list.Add(item);
					break;
				}
			}
		}
		return list;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild.ParentSlot == base.OutputSlot)
		{
			newChild.ScaleToSlot(0.95f);
		}
		IsOperable();
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			DynamicThing occupant = interaction.SourceSlot.Occupant;
			if (OnOff)
			{
				delayedActionInstance.ActionMessage = ActionStrings.Build;
				delayedActionInstance.AppendStateMessage(GameStrings.ThingCreateThing, ResultPrefab ? ResultPrefab.ToTooltip() : "Unknown", StringManager.Get(base.Completed * 100f));
				return delayedActionInstance.Fail();
			}
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
				IChemistryIngredient chemistryIngredient = occupant as IChemistryIngredient;
				delayedActionInstance.ActionMessage = ActionStrings.Add + " " + occupant.DisplayName;
				delayedActionInstance.Duration = 0f;
				if (OnOff)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceCanNotAddWhileProcessing);
					return delayedActionInstance.Fail();
				}
				if (chemistryIngredient == null)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceCanNotAdd);
					return delayedActionInstance.Fail();
				}
				if (chemistryIngredient.AddMixture.TotalReagents <= 0.0)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoUseAbleIngredients);
					return delayedActionInstance.Fail();
				}
				float num = chemistryIngredient.QuantityPerUse;
				IQuantity quantity = occupant as IQuantity;
				if (interaction.AltKey)
				{
					num = 1f;
				}
				if (quantity != null && quantity.GetQuantity < num)
				{
					num = quantity.GetQuantity;
				}
				ReagentMixture reagentMixture = ((num != chemistryIngredient.QuantityPerUse) ? (new ReagentMixture(chemistryIngredient.AddMixture) * (num / chemistryIngredient.QuantityPerUse)) : chemistryIngredient.AddMixture);
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceAddReagents, reagentMixture.TotalReagents.ToStringRounded(), chemistryIngredient.ToTooltip());
					return delayedActionInstance.Succeed();
				}
				if (interaction.SourceThing.RootParentHuman.IsLocalPlayer)
				{
					UIAudioManager.Play(UIAudioManager.ObjectPutHash);
				}
				if (GameManager.RunSimulation)
				{
					ReagentMixture.Add(reagentMixture);
					chemistryIngredient.OnUseItem(num, this);
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
}
