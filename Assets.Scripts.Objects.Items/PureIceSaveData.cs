using System.Collections.Generic;
using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

public class PureIceSaveData : OreSaveData
{
	[XmlElement("SpawnGas")]
	public List<SpawnContentsData> SpawnContentsDatas = new List<SpawnContentsData>();

	[XmlElement]
	public float Temperature;

	[XmlElement]
	public float MeltTemperature;
}
