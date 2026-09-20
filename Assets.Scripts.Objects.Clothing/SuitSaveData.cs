using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Clothing;

[XmlInclude(typeof(DynamicThingSaveData))]
public class SuitSaveData : AtmosphericItemSaveData
{
	[XmlElement]
	public float OutputSetting;

	[XmlElement]
	public float OutputTemperatureSetting = 293.15f;
}
