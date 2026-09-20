using System.Collections.Generic;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class AutomatedOven : SimpleFabricatorBase
{
	private bool isDone;

	private Dictionary<MachineTier, List<DynamicThing>> _DynamicThings = new Dictionary<MachineTier, List<DynamicThing>>();

	private List<DynamicThing> _validDynamicThings = new List<DynamicThing>();

	public static DynamicThingRecipeComparable RecipeComparable = new DynamicThingRecipeComparable("automatedoven");

	public override bool CanBeginImport
	{
		get
		{
			if (base.IsImportOpen && ImportingThing != null && !(ImportingThing is Ingot) && !(ImportingThing is Ore))
			{
				return ImportingThing is IIngredient;
			}
			return false;
		}
	}

	public override Dictionary<DynamicThing, Recipe> Recipes => RecipeComparable.AllRecipes;

	public override Dictionary<MachineTier, List<DynamicThing>> DynamicThings
	{
		get
		{
			if (!isDone)
			{
				PopulateDynamicThingsDict(ref _DynamicThings);
				isDone = true;
			}
			return _DynamicThings;
		}
	}

	public override List<DynamicThing> ValidDynamicThings
	{
		get
		{
			if (base.NeedsValidate)
			{
				RefreshDynamicThings();
			}
			return _validDynamicThings;
		}
	}

	public override Dictionary<int, int> PrefabTypeLookup => RecipeComparable.PrefabTypeLookup;

	public override bool CanProcess(Recipe recipe)
	{
		return MicrowaveIngredients.CanProcess(recipe);
	}

	public override bool CanProcess(Reagent reagentType)
	{
		return MicrowaveIngredients.CanProcess(reagentType);
	}

	public override List<Item> GetResourcesUsed()
	{
		return MicrowaveIngredients.GetResourcesUsed();
	}

	public override int CompletedProductionSoundHash()
	{
		return Animator.StringToHash("OvenCompleted");
	}

	public override void RefreshDynamicThings()
	{
		_validDynamicThings.Clear();
		GetValidDynamicThings(ref _validDynamicThings);
		base.NeedsValidate = false;
	}

	public new void GetValidDynamicThings(ref List<DynamicThing> list)
	{
		for (int i = 0; i < 4 && i <= (int)base.CurrentTier; i++)
		{
			if (DynamicThings.ContainsKey((MachineTier)i))
			{
				list.AddRange(DynamicThings[(MachineTier)i]);
			}
		}
	}

	public void PopulateDynamicThingsDict(ref Dictionary<MachineTier, List<DynamicThing>> _DynamicThings)
	{
		foreach (DynamicThing dynamicThing in RecipeComparable.DynamicThings)
		{
			if (!_DynamicThings.ContainsKey(dynamicThing.RecipeTier))
			{
				_DynamicThings[dynamicThing.RecipeTier] = new List<DynamicThing>();
				_DynamicThings[dynamicThing.RecipeTier].Add(dynamicThing);
			}
			else
			{
				_DynamicThings[dynamicThing.RecipeTier].Add(dynamicThing);
			}
		}
	}

	protected override void OnImportClosingComplete()
	{
		CollectResource(ImportingThing as IIngredient, ImportingThing);
		OnServer.Interact(base.InteractImport, 0);
	}

	public void CollectResource(IIngredient ingredient, DynamicThing importThing)
	{
		if (ingredient != null)
		{
			float num = ingredient.QuantityPerUse;
			if (ingredient is IQuantity quantity)
			{
				num = quantity.GetQuantity;
			}
			ReagentMixture reagentMixture = ((num != ingredient.QuantityPerUse) ? (new ReagentMixture(ingredient.AddMixture) * (num / ingredient.QuantityPerUse)) : ingredient.AddMixture);
			ReagentMixture.Add(reagentMixture);
			OnServer.Destroy(importThing);
		}
	}

	public override void PlayImportErrorSound(DynamicThing newChild)
	{
		if (!GameManager.IsBatchMode && newChild.ParentSlot == ImportSlot && !(newChild is IIngredient))
		{
			Singleton<AudioManager>.Instance.PlayAudioClipsData(this, FabricatorBase.ImportErrorHash, base.InteractOnOff.Collider.transform.localPosition);
		}
	}
}
