using System.Collections.Generic;
using System.Xml.Serialization;

namespace ThingImport.Thumbnails;

public class ThumbnailLinkGroup
{
	[XmlAttribute]
	public string Leader;

	[XmlArray("Members")]
	[XmlArrayItem("Prefab")]
	public List<string> Members = new List<string>();
}
