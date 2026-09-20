using Reagents;

namespace Assets.Scripts.Objects.Appliances;

public interface IIngredient
{
	ReagentMixture AddMixture { get; }

	float QuantityPerUse { get; }

	ReagentMixture GetTotalReagentMixture();

	string ToTooltip();

	bool OnUseItem(float quantity, Thing onUseThing);

	int GetPrefabHash();

	string GetPrefabName();
}
