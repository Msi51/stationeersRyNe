using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Util;
using Objects.Items;
using Reagents;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class Ingot : Consumable, IProcessable, IIngredient, IChemistryIngredient
{
	[HideInInspector]
	public IngotType IngotType;

	public static List<Ingot> AllSuperAlloyPrefabs = new List<Ingot>();

	public static List<Ingot> AllAlloyPrefabs = new List<Ingot>();

	public static List<Ingot> AllIngotPrefabs = new List<Ingot>();

	public static IngotRecipeComparable RecipeComparable = new IngotRecipeComparable("Ingot");

	public static Dictionary<Recipe, Ingot> Recipes = new Dictionary<Recipe, Ingot>();

	public float GetProcessedQuantity => UseAmount / MaxQuantity;

	public override float GetTradableQuantity => GetQuantity;

	public override string GetQuantityText()
	{
		return StringGenerator.GetString((int)base.Quantity, Unit.g);
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (IngotType != IngotType.Basic)
		{
			extendedText.AppendLine(GameStrings.IngotIsType.AsString(EnumCollections.IngotTypes.GetName(IngotType)));
		}
		return extendedText;
	}

	public override float CalculateUniqueRatioIdentifier()
	{
		return base.Quantity;
	}
}
