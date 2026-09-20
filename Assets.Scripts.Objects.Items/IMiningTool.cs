using Assets.Scripts.Voxel;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public interface IMiningTool : IReferencable, IEvaluable
{
	CursorVoxelMode CursorVoxelMode { get; }

	void OnMinedOre(Ore oreMined);

	void OnMinedVoxel(float density);

	bool IsAvailable();

	float GetMineCompletionTime();

	float GetMineAmount();

	static void MineEffect(Vector3 position, MinableType minedType, float density)
	{
		if ((int)minedType >= 1 && (int)minedType < 255 && density > 0f)
		{
			EffectManager.CreatePickingEffect(position, density);
		}
	}
}
