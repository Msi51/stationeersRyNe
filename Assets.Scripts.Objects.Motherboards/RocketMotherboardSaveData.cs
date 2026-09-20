using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

[XmlInclude(typeof(RocketMotherboardSaveData))]
public class RocketMotherboardSaveData : MotherboardSaveData
{
	[XmlElement]
	public List<long> PinnedDevices;

	[XmlElement]
	public List<long> PinnedLogicValueDevice;

	[XmlElement]
	public List<byte> PinnedLogicValueType;
}
