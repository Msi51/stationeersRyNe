using System.Xml.Serialization;
using UnityEngine;

namespace ThingImport;

public class SpriteReference
{
	[XmlAttribute]
	public string Path;

	[XmlIgnore]
	public Sprite Sprite;

	public void Load()
	{
		if (!string.IsNullOrEmpty(Path))
		{
			Sprite = StreamingAssetLoader.LoadSprite(this);
		}
	}
}
