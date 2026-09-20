namespace Assets.Scripts.Objects.Items;

public interface IHydration
{
	float Hydration(float useAmount);

	Thing.DelayedActionInstance OnUseSecondary(bool doAction = false, float actionCompletedRatio = 1f);

	bool OnUseItem(float quantity, Thing useOnThing);

	float HydrateAmount(Entity consumer);

	float HydrateTime(float quantityToDrink);
}
