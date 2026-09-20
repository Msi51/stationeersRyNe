using TMPro;

namespace Assets.Scripts.UI;

public class StationpediaSuitInfoPanel : GameBase
{
	public TextMeshProUGUI MovementSpeed;

	public TextMeshProUGUI CoolantTemperatureRange;

	public TextMeshProUGUI OperatingTemperatureRange;

	public TextMeshProUGUI MaxOperatingTemperatureRange;

	public void SetValues(StationpediaPage page)
	{
		if (page.StationSuitInfo != null)
		{
			CoolantTemperatureRange.text = page.StationSuitInfo.CoolantTemperatureRange;
			OperatingTemperatureRange.text = page.StationSuitInfo.OperatingTemperatureRange;
			MaxOperatingTemperatureRange.text = page.StationSuitInfo.MaxOperatingTemperatureRange;
			MovementSpeed.text = page.StationSuitInfo.MovementSpeed;
		}
	}
}
