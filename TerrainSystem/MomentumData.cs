namespace TerrainSystem;

public class MomentumData : VeinModifierData
{
	public float GetMomentumBias(int depth)
	{
		return VeinModifierData.CalculateModifiedValue(Value, depth, DepthModifiers);
	}
}
