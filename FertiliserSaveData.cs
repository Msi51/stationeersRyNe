using System.Xml.Serialization;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;

[XmlInclude(typeof(ThingSaveData))]
public class FertiliserSaveData : StackableSaveData
{
	[XmlElement]
	public float Cycles;

	[XmlElement]
	public float HarvestBoost;

	[XmlElement]
	public float GrowthSpeed;
}
