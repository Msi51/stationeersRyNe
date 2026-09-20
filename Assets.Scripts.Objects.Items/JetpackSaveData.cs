using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(DynamicThingSaveData))]
public class JetpackSaveData : AtmosphericItemSaveData
{
	[XmlElement]
	public float OutputSetting = 0.5f;
}
