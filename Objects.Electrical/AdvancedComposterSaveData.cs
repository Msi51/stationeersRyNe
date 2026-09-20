using System.Xml.Serialization;
using Assets.Scripts.Objects.Pipes;

namespace Objects.Electrical;

[XmlInclude(typeof(AdvancedComposterSaveData))]
public class AdvancedComposterSaveData : DeviceInputOutputImportExportSaveData
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
