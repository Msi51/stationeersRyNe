using System.Xml.Serialization;
using UnityEngine;

namespace ThingImport;

public class TextureReference
{
	[XmlAttribute("Path")]
	public string Path;

	[XmlIgnore]
	public int PathHash;

	[XmlElement("Tiling")]
	public Vector2Reference Tiling;

	[XmlAttribute("Format")]
	public TextureFormat TextureFormat = TextureFormat.DXT5;

	[XmlAttribute("Linear")]
	public bool Linear;

	[XmlAttribute("MipMapped")]
	public bool MipMapped = true;

	[XmlAttribute("LoadType")]
	public TextureLoadType TextureLoadType = TextureLoadType.Preload;

	protected Texture2D _texture;

	[XmlIgnore]
	public virtual Texture2D Texture
	{
		get
		{
			if (IsLoaded())
			{
				return _texture;
			}
			_texture = StreamingAssetLoader.LoadTexture(this, TextureFormat);
			return _texture;
		}
	}

	public bool IsLoaded()
	{
		return StreamingAssetLoader.IsLoaded(this);
	}

	public virtual void Load()
	{
		PathHash = Animator.StringToHash(Path);
		if (!string.IsNullOrEmpty(Path) && TextureLoadType == TextureLoadType.Preload)
		{
			_texture = StreamingAssetLoader.LoadTexture(this, TextureFormat, Linear);
		}
	}
}
