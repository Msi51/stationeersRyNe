using System.Collections.Generic;
using Reagents;

namespace Assets.Scripts.Objects.Items;

public class RecipeReference
{
	public Recipe Recipe;

	public DynamicThing DynamicThing;

	public DynamicThing Source;

	public Thing Creator;

	public RecipeReference(KeyValuePair<Recipe, IQuantity> record, string creator)
	{
		Recipe = record.Key;
		DynamicThing = (DynamicThing)record.Value;
		Creator = Prefab.Find(creator);
	}

	public RecipeReference(KeyValuePair<Recipe, Ingot> record, string creator)
	{
		Recipe = record.Key;
		DynamicThing = record.Value;
		Creator = Prefab.Find(creator);
	}

	public RecipeReference(KeyValuePair<Recipe, Item> record, string creator)
	{
		Recipe = record.Key;
		DynamicThing = record.Value;
		Creator = Prefab.Find(creator);
	}

	public RecipeReference(KeyValuePair<Recipe, Ore> record, string creator)
	{
		Recipe = record.Key;
		DynamicThing = record.Value;
		Creator = Prefab.Find(creator);
	}

	public RecipeReference(KeyValuePair<DynamicThing, Recipe> record, string creator)
	{
		Recipe = record.Value;
		DynamicThing = record.Key;
		Creator = Prefab.Find(creator);
	}

	public RecipeReference(DynamicThing thing, Recipe record, string creator)
	{
		Recipe = record;
		DynamicThing = thing;
		Creator = Prefab.Find(creator);
	}

	public RecipeReference(KeyValuePair<int, Item> record, string creator)
	{
		Recipe = default(Recipe);
		Source = Prefab.Find(record.Key) as DynamicThing;
		DynamicThing = record.Value;
		Creator = Prefab.Find(creator);
	}
}
