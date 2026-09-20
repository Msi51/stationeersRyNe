using System.Xml.Serialization;
using UnityEngine;

namespace ThingImport;

public class NormalMapReference : TextureReferenceWithValue
{
	[XmlIgnore]
	public override Texture2D Texture
	{
		get
		{
			if (IsLoaded())
			{
				return _texture;
			}
			_texture = StreamingAssetLoader.LoadNormalMap(this);
			return _texture;
		}
	}

	public override void Load()
	{
		PathHash = Animator.StringToHash(Path);
		if (!string.IsNullOrEmpty(Path) && TextureLoadType == TextureLoadType.Preload)
		{
			_texture = StreamingAssetLoader.LoadNormalMap(this);
		}
	}
}
