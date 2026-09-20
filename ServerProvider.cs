using System.Xml.Serialization;
using Assets.Scripts;
using ThingImport;

public class ServerProvider : DataCollection
{
	[XmlElement("Url")]
	public StringReference Url;

	[XmlElement("Logo")]
	public TextureReference Logo;

	public override void Initialize(ModAbout mod)
	{
		Logo.Load();
	}
}
