using System.Xml.Serialization;

public abstract class SettingBase
{
	[XmlAttribute("Id")]
	public string Id;

	public static implicit operator string(SettingBase serializedData)
	{
		return serializedData.Id;
	}
}
