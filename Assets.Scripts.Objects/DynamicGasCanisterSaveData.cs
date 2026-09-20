using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

[XmlInclude(typeof(DynamicThingSaveData))]
public class DynamicGasCanisterSaveData : DynamicThingSaveData
{
	[XmlElement]
	public float OutputSetting;
}
