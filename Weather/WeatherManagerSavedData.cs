using System.Xml.Serialization;

namespace Weather;

[XmlRoot]
public class WeatherManagerSavedData
{
	[XmlElement]
	public string CurrentWeatherEventId;

	[XmlElement]
	public bool IsWeatherEventRunning;

	[XmlElement]
	public bool IsWeatherEventScheduled;

	[XmlElement("WeatherStartTime")]
	public float WeatherStartTimeOffset;

	[XmlElement]
	public float WeatherEventLength;

	[XmlElement]
	public int DaysSinceLastWeatherEvent;

	[XmlElement]
	public int LastEventCoolDown;
}
