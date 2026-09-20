using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts;

[XmlRoot("TerrainProps")]
public class TerrainProps
{
	[XmlElement("TerrainProp")]
	public List<LavaLakeProp> LavaLakeList;
}
