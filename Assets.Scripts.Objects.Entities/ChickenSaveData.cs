using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Entities;

[XmlInclude(typeof(DynamicThingSaveData))]
public class ChickenSaveData : EntitySaveData
{
	[XmlElement]
	public float EggTimeDefault = float.NaN;

	[XmlElement]
	public float LayEggTimer;
}
