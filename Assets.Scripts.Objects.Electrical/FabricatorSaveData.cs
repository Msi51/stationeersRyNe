using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(StructureSaveData))]
public class FabricatorSaveData : StructureSaveData
{
	[XmlArray("Jobs")]
	[XmlArrayItem("Job")]
	public List<FabricatorJob> FabricatorJobs = new List<FabricatorJob>();

	public FabricatorJob CurrentJob;

	public float Progress;
}
