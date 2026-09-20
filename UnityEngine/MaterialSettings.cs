using System.Xml.Serialization;

namespace UnityEngine;

public class MaterialSettings
{
	[XmlElement("Macro")]
	public MacroTextureData MacroTextureData { get; set; }

	[XmlElement("Detail")]
	public DetailTextureData DetailTextureData { get; set; }

	public void Initialize(ModAbout mod)
	{
		MacroTextureData.Initialize(mod);
		DetailTextureData.Initialize(mod);
	}
}
