using UnityEngine;

namespace TerrainSystem;

public class ThicknessData : VeinModifierData
{
	public int GetThickness(int depth)
	{
		return Mathf.RoundToInt(VeinModifierData.CalculateModifiedValue(Value, depth, DepthModifiers));
	}
}
