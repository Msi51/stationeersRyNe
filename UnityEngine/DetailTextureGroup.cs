using System;
using System.Xml.Serialization;
using Assets.Scripts;
using ThingImport;

namespace UnityEngine;

public class DetailTextureGroup
{
	[XmlElement("Albedo")]
	public TextureReference Albedo { get; set; }

	[XmlElement("Normal")]
	public NormalMapReference Normal { get; set; }

	[XmlElement("Displacement")]
	public TextureReferenceWithValue Displacement { get; set; }

	[XmlElement("Roughness")]
	public TextureReferenceWithValue Roughness { get; set; }

	[XmlElement("Emissive")]
	public TextureReferenceWithValue Emissive { get; set; }

	public void Load()
	{
		Albedo?.Load();
		Normal?.Load();
		Displacement?.Load();
		Roughness?.Load();
		Emissive?.Load();
	}

	public Texture2D GetPackedAlbedoAndEmissive()
	{
		if (Emissive == null)
		{
			return Albedo?.Texture;
		}
		if ((object)Albedo?.Texture == null)
		{
			ConsoleWindow.PrintError("Terrain shader requires a diffuse texture to be set.");
			throw new ArgumentNullException("Albedo", "Albedo texture is required for terrain shader.");
		}
		Texture2D texture2D = new Texture2D(Albedo.Texture.width, Albedo.Texture.height, TextureFormat.RGBA32, mipChain: false);
		Color32[] pixels = Albedo.Texture.GetPixels32();
		Color32[] pixels2 = Emissive.Texture.GetPixels32();
		if (pixels.Length != pixels2.Length)
		{
			ConsoleWindow.PrintError("Diffuse and emissive textures must be the same size. Emissive texture is being discarded.");
			return Albedo.Texture;
		}
		Color32[] array = new Color32[pixels.Length];
		for (int i = 0; i < pixels.Length; i++)
		{
			array[i] = new Color32(pixels[i].r, pixels[i].g, pixels[i].b, pixels2[i].r);
		}
		texture2D.SetPixels32(array);
		texture2D.Apply();
		return texture2D;
	}

	public object GetPackedDisplacementAndRoughness()
	{
		throw new NotImplementedException();
	}
}
