using System.Xml.Serialization;

namespace UnityEngine;

public class DetailTextureData
{
	[XmlAttribute("UVScale")]
	public float UVScale = 1f;

	[XmlAttribute("DetailFadeStart")]
	public float DetailFadeStart = 50f;

	[XmlAttribute("DetailFadeEnd")]
	public float DetailFadeEnd = 200f;

	[XmlAttribute("FractalDepthScale")]
	public float FractalDepthScale = 1f;

	[XmlAttribute("LodMin")]
	public float LodMin = 2f;

	[XmlAttribute("MipmapBias")]
	public float MipmapBias = -0.1f;

	[XmlAttribute("BlendSharpness")]
	public float BlendSharpness { get; set; }

	[XmlElement("GroupOne")]
	public DetailTextureGroup GroupOne { get; set; }

	[XmlElement("GroupTwo")]
	public DetailTextureGroup GroupTwo { get; set; }

	public void Initialize(ModAbout mod)
	{
		GroupOne?.Load();
		GroupTwo?.Load();
	}
}
