using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(DynamicThingSaveData))]
public class MotherboardSaveData : DynamicThingSaveData
{
	[XmlArray]
	public long[] LinkedDeviceReferences;

	[XmlElement]
	public int Flag;

	[XmlElement]
	public long MasterMotherboard;
}
