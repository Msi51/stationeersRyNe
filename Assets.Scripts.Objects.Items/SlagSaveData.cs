using System.Collections.Generic;
using System.Xml.Serialization;
using Reagents;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(ThingSaveData))]
public class SlagSaveData : StackableSaveData
{
	[XmlArray("CreatedReagents")]
	[XmlArrayItem("Reagent")]
	public List<ReagentSaveData> CreatedReagents = new List<ReagentSaveData>();
}
