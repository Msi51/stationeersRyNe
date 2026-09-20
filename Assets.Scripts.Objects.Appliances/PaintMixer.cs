using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using Reagents;
using Trading;

namespace Assets.Scripts.Objects.Appliances;

public class PaintMixer : ApplianceReagentImportBase, IResourceConsumer, IReferencable, IEvaluable
{
	public static List<IPaintMixerIngredient> AllIngredients = new List<IPaintMixerIngredient>();

	public static ItemRecipeComparable RecipeComparable = new ItemRecipeComparable("PaintMixer");

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
		foreach (IPaintMixerIngredient allIngredient in AllIngredients)
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
		foreach (IPaintMixerIngredient allIngredient in AllIngredients)
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
		foreach (IPaintMixerIngredient allIngredient in AllIngredients)
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
				delayedActionInstance.AppendStateMessage(GameStrings.ThingCreateThing, ResultPrefab ? ResultPrefab.ToTooltip() : "???", StringManager.Get(base.Completed * 100f));
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
				IPaintMixerIngredient paintMixerIngredient = occupant as IPaintMixerIngredient;
				delayedActionInstance.ActionMessage = ActionStrings.Add + " " + occupant.DisplayName;
				delayedActionInstance.Duration = 0f;
				if (OnOff)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceCanNotAddWhileProcessing);
					return delayedActionInstance.Fail();
				}
				if (paintMixerIngredient == null)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceCanNotAdd);
					return delayedActionInstance.Fail();
				}
				if (paintMixerIngredient.AddMixture.TotalReagents <= 0.0)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceNoUseAbleIngredients);
					return delayedActionInstance.Fail();
				}
				float num = paintMixerIngredient.QuantityPerUse;
				IQuantity quantity = occupant as IQuantity;
				if (interaction.AltKey)
				{
					num = 1f;
				}
				if (quantity != null && quantity.GetQuantity < num)
				{
					num = quantity.GetQuantity;
				}
				ReagentMixture reagentMixture = ((num != paintMixerIngredient.QuantityPerUse) ? (new ReagentMixture(paintMixerIngredient.AddMixture) * (num / paintMixerIngredient.QuantityPerUse)) : paintMixerIngredient.AddMixture);
				if (!doAction)
				{
					delayedActionInstance.AppendStateMessage(GameStrings.DeviceAddReagents, reagentMixture.TotalReagents.ToStringRounded(), paintMixerIngredient.ToTooltip());
					return delayedActionInstance.Succeed();
				}
				if (GameManager.RunSimulation)
				{
					ReagentMixture.Add(reagentMixture);
					paintMixerIngredient.OnUseItem(num, this);
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

	public override void Awake()
	{
		base.Awake();
		base.OutputSlot.IsInteractable = false;
		base.OutputSlot.Interactable.Collider.enabled = false;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (newChild.ParentSlot == base.OutputSlot)
		{
			base.OutputSlot.Interactable.Collider.enabled = true;
			base.OutputSlot.IsInteractable = true;
		}
		IsOperable();
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		base.OnChildExitInventory(previousChild);
		if (base.OutputSlot.Occupant == null)
		{
			base.OutputSlot.IsInteractable = false;
			base.OutputSlot.Interactable.Collider.enabled = false;
		}
		IsOperable();
	}
}
