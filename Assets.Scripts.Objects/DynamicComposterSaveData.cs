using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

[XmlInclude(typeof(DynamicComposterSaveData))]
public class DynamicComposterSaveData : DynamicThingSaveData
{
	[XmlElement]
	public int UnprocessedItems;

	[XmlElement]
	public int SavedDecayFoodQuantity;

	[XmlElement]
	public int SavedNormalFoodQuantity;

	[XmlElement]
	public int SavedBiomassQuantity;
}
