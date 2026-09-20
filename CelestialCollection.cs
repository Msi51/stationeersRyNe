using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("CelestialBodies")]
public class CelestialCollection
{
	[XmlIgnore]
	private bool _initialized;

	[XmlElement("Body", Type = typeof(CelestialBodyReference))]
	[XmlElement("Sprite", Type = typeof(CelestialSpriteReference))]
	[XmlElement("RockyBody", Type = typeof(RockyBodyReference))]
	[XmlElement("AtmosphericBody", Type = typeof(AtmosphericBodyReference))]
	public List<CelestialBodyReference> Bodies = new List<CelestialBodyReference>();

	public void LoadData()
	{
		if (_initialized)
		{
			return;
		}
		foreach (CelestialBodyReference body in Bodies)
		{
			body.LoadData();
		}
		_initialized = true;
	}

	public CelestialBodyReference Find(string playableBodyId)
	{
		foreach (CelestialBodyReference body in Bodies)
		{
			if (body.Id == playableBodyId)
			{
				return body;
			}
		}
		return null;
	}
}
