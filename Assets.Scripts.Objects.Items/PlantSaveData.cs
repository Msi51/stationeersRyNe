using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Genetics;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(ThingSaveData))]
public class PlantSaveData : StackableSaveData
{
	[XmlElement]
	public float StageTime;

	[XmlElement]
	public int Stage;

	[XmlElement]
	public int HarvestQuantity = -1;

	[XmlElement]
	public int SeedQuantity = -1;

	[XmlElement]
	public float FertilizerBoost = 1f;

	[XmlElement]
	public bool IsFertilized;

	[XmlElement]
	public string PlanterName;

	[XmlArray("StackedGeneCollections")]
	[XmlArrayItem("GeneCollections")]
	public List<GeneCollectionWrapper> StackedGeneCollectionWrappers = new List<GeneCollectionWrapper>();

	[XmlElement]
	public PlantRecord PlantRecord;

	[XmlArray("AggregateStates")]
	[XmlArrayItem("State")]
	public List<StateWrapper> AggregateStates = new List<StateWrapper>();

	[XmlArray("CurrentStates")]
	[XmlArrayItem("State")]
	public List<BooleanStateWrapper> CurrentStates = new List<BooleanStateWrapper>();
}
