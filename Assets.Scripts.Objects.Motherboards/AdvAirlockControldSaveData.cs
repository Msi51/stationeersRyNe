using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

[XmlInclude(typeof(CircuitboardSaveData))]
public class AdvAirlockControldSaveData : CircuitboardSaveData
{
	[XmlElement]
	public float PressureInternal = 101f;

	[XmlElement]
	public float PressureExternal = 101f;
}
