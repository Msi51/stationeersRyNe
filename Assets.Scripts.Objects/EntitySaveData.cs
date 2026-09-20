using System.Xml.Serialization;
using Assets.Scripts.Objects.Entities;

namespace Assets.Scripts.Objects;

[XmlInclude(typeof(DynamicThingSaveData))]
public class EntitySaveData : DynamicThingSaveData
{
	[XmlElement]
	public EntityState State;

	[XmlElement]
	public float Oxygenation;

	[XmlElement]
	public float Nutrition;

	[XmlElement]
	public float Hydration = 5f;

	[XmlElement]
	public float Mood = 1f;

	[XmlElement]
	public float Hygiene = 1f;

	[XmlElement]
	public float FoodQuality = 0.75f;

	[XmlElement]
	public float RespawnStressTime;

	[XmlElement]
	public int MovementControllerControlMode;

	[XmlElement]
	public ushort DaysLived;

	[XmlElement]
	public FloatReference CurrentDecayTime;
}
