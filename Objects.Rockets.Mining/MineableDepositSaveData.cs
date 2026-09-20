using System.Xml.Serialization;

namespace Objects.Rockets.Mining;

public class MineableDepositSaveData
{
	[XmlElement]
	public float Density;

	[XmlElement]
	public float Richness;

	[XmlElement]
	public float Size;

	[XmlElement]
	public uint MinedQuantityTotal;

	[XmlElement]
	public uint TotalOreAtLocation;

	public MineableDepositSaveData()
	{
	}

	public MineableDepositSaveData(MineableDeposit deposit)
	{
		Density = deposit.Density;
		Richness = deposit.Richness;
		Size = deposit.Size;
		MinedQuantityTotal = deposit.MinedQuantityTotal;
		TotalOreAtLocation = deposit.TotalOreAtLocation;
	}
}
