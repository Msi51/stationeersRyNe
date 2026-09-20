using System.Xml.Serialization;
using Assets.Scripts;
using ThingImport;

namespace UnityEngine;

[XmlRoot("TerrainSettings")]
public class TerrainSettings : DataCollection
{
	[XmlAttribute("Path")]
	public string RelativePath;

	[XmlAttribute("WorldSize")]
	public int Size;

	[XmlElement("Curvature")]
	public FloatReference Curvature;

	[XmlElement("MiniMap")]
	public TextureReference MiniMapTexture;

	[XmlElement("Lava")]
	public LavaData LavaData;

	[XmlElement("TerrainProps")]
	public TerrainProps TerrainProps;

	[XmlElement("MaterialSettings")]
	public MaterialSettings MaterialSettings { get; set; }

	public override void Initialize(ModAbout mod)
	{
		MaterialSettings?.Initialize(mod);
		LavaData?.Initialize(mod);
		if (Curvature != null)
		{
			Curvature.Value = Mathf.Clamp01(Curvature);
		}
	}
}
