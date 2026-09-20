using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

[XmlInclude(typeof(CircuitboardSaveData))]
public class AirContolCircuitboardSaveData : CircuitboardSaveData
{
	[XmlArray("AirControlVents")]
	[XmlArrayItem("ActiveVent")]
	public List<AirControlVent> AirControlVents = new List<AirControlVent>();
}
