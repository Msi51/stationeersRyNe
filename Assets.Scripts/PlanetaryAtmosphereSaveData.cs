using System.Xml.Serialization;

namespace Assets.Scripts;

public class PlanetaryAtmosphereSaveData
{
	[XmlElement("GlobalGasMix")]
	public GlobalGasMixSaveData GlobalGasMix;

	[XmlElement("LiquidClouds")]
	public GlobalGasMixSaveData LiquidClouds;

	[XmlElement("IceClouds")]
	public GlobalGasMixSaveData IceClouds;

	[XmlElement("IceCaps")]
	public GlobalGasMixSaveData IceCaps;

	[XmlElement("LatentEnergyOffset")]
	public DoubleReference LatentOffset;

	[XmlElement("EnergyOffset")]
	public DoubleReference ExternalOffset;
}
