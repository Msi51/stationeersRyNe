using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class ChuteDigitalFlipFlopSaveData : ChuteDeviceSaveData
{
	public int Setting;

	public int Setting2;

	public int Quantity;
}
