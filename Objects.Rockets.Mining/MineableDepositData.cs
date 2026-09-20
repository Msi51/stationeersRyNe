using System.Xml.Serialization;
using Assets.Scripts;
using Trading;

namespace Objects.Rockets.Mining;

public class MineableDepositData
{
	[XmlAttribute("Id")]
	public string Id;

	[XmlElement("Density")]
	public IntRangeData DensityData;

	[XmlElement("Richness")]
	public IntRangeData RichnessData;

	[XmlElement("Size")]
	public IntRangeData SizeData;

	[XmlElement("Composition")]
	public DepositCompositionData DepositCompositionData;

	public bool Validate()
	{
		if (DepositCompositionData == null)
		{
			ConsoleWindow.PrintError("SpaceMapData Error: Minable Deposit " + Id + " has no composition data");
			return false;
		}
		return DepositCompositionData.Validate();
	}
}
