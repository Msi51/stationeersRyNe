using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

[XmlInclude(typeof(MapMotherboardSaveData))]
public class MapMotherboardSaveData : MotherboardSaveData
{
	[XmlElement]
	public string MaskTexture;

	[XmlElement]
	public bool DeepMinablesToggle;

	[XmlElement]
	public bool PlayersToggle;
}
