using System.Xml.Serialization;
using Assets.Scripts.Networking.Transports;

public class WorkshopModData : ModData
{
	[XmlElement("WorkshopId")]
	public UnsignedLongReference WorkshopId = new UnsignedLongReference();

	public WorkshopModData()
	{
	}

	public WorkshopModData(SteamTransport.ItemWrapper itemWrapper, bool isEnabled)
	{
		WorkshopId = new UnsignedLongReference(itemWrapper.Id);
		Enabled = isEnabled;
		DirectoryPath = new PathReference(itemWrapper.DirectoryPath);
	}
}
