namespace Assets.Scripts.Objects.Items;

public class SPUCelestialScanner : SensorProcessingUnit
{
	public static bool ShowVisualization;

	public override void Render()
	{
		ShowVisualization = true;
	}
}
