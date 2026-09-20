using System.Xml.Serialization;
using Assets.Scripts.Objects;
using UnityEngine;

[XmlRoot]
public class SPDAHomePageButtonOverride
{
	[XmlElement]
	public string CategoryName;

	[XmlElement]
	public string ThingNameForImage;

	[XmlElement]
	public string LinkedPageKey;

	[XmlIgnore]
	public SPDAHomePageCategory Prefab { get; set; }

	public Sprite GetImage()
	{
		return Assets.Scripts.Objects.Prefab.Find(ThingNameForImage)?.GetThumbnail();
	}
}
