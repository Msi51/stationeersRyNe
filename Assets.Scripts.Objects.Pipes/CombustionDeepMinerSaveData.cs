using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(DeepMinerSaveData))]
public class CombustionDeepMinerSaveData : DeepMinerSaveData
{
	[XmlElement]
	public float Throttle;

	[XmlElement]
	public float CombustionLimiter;

	[XmlElement]
	public float Rpm;

	[XmlElement]
	public float Stress;
}
