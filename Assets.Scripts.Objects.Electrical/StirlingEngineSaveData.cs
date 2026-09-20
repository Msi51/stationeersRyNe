using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Pipes;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StirlingEngineSaveData))]
public class StirlingEngineSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public AtmosphereSaveData HotInputAtmosphere;

	[XmlElement]
	public AtmosphereSaveData HotSideAtmosphere;

	[XmlElement]
	public AtmosphereSaveData ColdSideAtmosphere;
}
