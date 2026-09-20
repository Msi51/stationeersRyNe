using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;

namespace Assets.Scripts.Objects.Items;

[XmlRoot("SpawnGas")]
public class SpawnContentsData
{
	[XmlElement]
	public Chemistry.GasType Gastype;

	[XmlElement]
	public float Quantity;
}
