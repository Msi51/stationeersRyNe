using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Assets.Scripts.Serialization;

[XmlRoot("ModMetadata")]
public class ModAbout
{
	public const string ROOT_NAME = "ModMetadata";

	[XmlElement]
	public string Name;

	[XmlElement]
	public string Author;

	[XmlElement]
	public string Version;

	[XmlElement]
	public string Description;

	[XmlElement]
	public ulong WorkshopHandle;

	[XmlArray("Tags")]
	[XmlArrayItem("Tag")]
	public List<string> Tags;

	[XmlIgnore]
	public bool IsValid = true;

	public static ModAbout Load(string xmlFile)
	{
		int num = xmlFile.IndexOf("gamedata", StringComparison.InvariantCultureIgnoreCase);
		if (num == -1)
		{
			return null;
		}
		string path = Path.Combine(xmlFile.Substring(0, num), "About", "About.xml");
		if (!File.Exists(path))
		{
			return null;
		}
		return XmlSerialization.Deserialize<ModAbout>(path);
	}
}
