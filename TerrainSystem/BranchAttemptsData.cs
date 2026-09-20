using UnityEngine;

namespace TerrainSystem;

public class BranchAttemptsData : VeinModifierData
{
	public int GetBranchAttempts(int depth)
	{
		return Mathf.RoundToInt(VeinModifierData.CalculateModifiedValue(Value, depth, DepthModifiers));
	}
}
