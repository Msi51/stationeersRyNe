using Objects.Items;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class IngredientBase : Consumable
{
	public int TotalIngredient => Mathf.RoundToInt(base.Quantity);

	public override string GetQuantityText()
	{
		return $"{DisplayName} x {TotalIngredient}g";
	}
}
