using System.Xml.Serialization;

public class RayMarchData
{
	[XmlAttribute("Steps")]
	public int Steps = 32;

	[XmlAttribute("Size")]
	public float StepSize = 0.1f;

	[XmlAttribute("Growth")]
	public float StepGrowth;

	[XmlAttribute("Density")]
	public float DensityMultiplier = 1f;
}
