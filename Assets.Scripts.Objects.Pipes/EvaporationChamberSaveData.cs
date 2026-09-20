using System.Xml.Serialization;
using Assets.Scripts.Networks;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeviceAtmosphericSaveData))]
public class EvaporationChamberSaveData : DeviceAtmosphericSaveData
{
	[XmlElement]
	public long PipeNetworkId;

	[XmlElement]
	public PipeBurst IsBurst;

	public override bool IsValidData()
	{
		return base.IsValidData();
	}
}
