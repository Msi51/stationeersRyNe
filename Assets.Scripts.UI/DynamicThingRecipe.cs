using Assets.Scripts.Objects;
using Reagents;

namespace Assets.Scripts.UI;

public struct DynamicThingRecipe(DynamicThing thing, Recipe recipe)
{
	public DynamicThing Thing = thing;

	public Recipe Recipe = recipe;
}
