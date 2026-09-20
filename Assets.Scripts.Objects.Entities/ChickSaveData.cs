using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Entities;

[XmlInclude(typeof(DynamicThingSaveData))]
public class ChickSaveData : EntitySaveData
{
	[XmlElement]
	public float CurrentGrowthTime;
}
