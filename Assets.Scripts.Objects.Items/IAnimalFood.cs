using Assets.Scripts.GridSystem;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public interface IAnimalFood
{
	Vector3 Position { get; }

	Grid3 GridPosition { get; }

	float GetNutritionValue();

	bool OnUseItem(float quantity, Thing onUseThing);
}
