using Assets.Scripts.Objects.Appliances;

namespace Assets.Scripts.Objects.Items;

public interface IIngredientLabelled : IMicrowaveIngredient, IIngredient
{
	string GetStandardUnit();
}
