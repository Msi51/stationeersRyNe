using System.Xml.Serialization;

[XmlRoot("PlayableBody")]
public class PlayableBodyReference : CelestialReference
{
	public static PlayableBodyReference Default = new PlayableBodyReference
	{
		Id = "Space"
	};

	[XmlAttribute]
	public float Latitude = float.NaN;

	[XmlAttribute]
	public float Longitude = float.NaN;

	public float GetLatitude()
	{
		if (float.IsNaN(Latitude))
		{
			return 0f;
		}
		return Latitude;
	}

	public float GetLongitude()
	{
		if (float.IsNaN(Longitude))
		{
			return 0f;
		}
		return Longitude;
	}
}
