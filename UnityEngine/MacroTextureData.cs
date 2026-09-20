using System.Xml.Serialization;
using ThingImport;

namespace UnityEngine;

public class MacroTextureData
{
	[XmlElement("MacroTextureBlend")]
	public FloatReference MacroTextureBlend = new FloatReference(1f);

	[XmlElement("Albedo")]
	public TextureReference Albedo { get; set; }

	[XmlElement("Normal")]
	public NormalMapReference Normal { get; set; }

	public void Initialize(ModAbout mod)
	{
		Albedo?.Load();
		Normal?.Load();
	}
}
