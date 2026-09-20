namespace Assets.Scripts.Objects.Appliances;

public interface IProcessable : IIngredient
{
	float GetProcessedQuantity { get; }

	float GetQuantity { get; }

	float GetMaxQuantity { get; }

	void SetQuantity(float quantity);

	new int GetPrefabHash();
}
