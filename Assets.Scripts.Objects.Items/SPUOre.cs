using TerrainSystem;

namespace Assets.Scripts.Objects.Items;

public class SPUOre : SensorProcessingUnit
{
	public const int VIEW_DISTANCE = 48;

	public override void Render()
	{
		if (!GameManager.IsBatchMode)
		{
			VoxelTerrain.Read.RenderVisualisers();
		}
	}
}
