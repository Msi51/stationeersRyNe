using System.Xml.Serialization;
using Assets.Scripts;

public class StormEffectData
{
	[XmlAttribute("Speed")]
	public float Speed;

	[XmlAttribute("Size")]
	public float Size;

	[XmlElement("RayMarch")]
	public RayMarchData RayMarchData = new RayMarchData();

	[XmlElement("TextureId")]
	public IntReference TextureIndex = new IntReference(0);

	[XmlElement("Layer1")]
	public NoiseLayerData Layer1;

	[XmlElement("Layer2")]
	public NoiseLayerData Layer2;

	[XmlElement("Layer3")]
	public NoiseLayerData Layer3;

	[XmlElement("Color1")]
	public Color32Reference Color1;

	[XmlElement("Color2")]
	public Color32Reference Color2;

	[XmlElement("MaskColor")]
	public Color32Reference MaskColor;

	[XmlElement("EmissiveMult")]
	public FloatReference EmissiveMult = new FloatReference(1f);

	public bool IsValid()
	{
		if (Layer1 != null && Layer2 != null && Color1 != null)
		{
			return Color2 != null;
		}
		return false;
	}
}
