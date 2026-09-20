using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public class LogicPidControllerSaveData : LogicReaderSaveData
{
	[XmlElement]
	public double SetPoint;

	[XmlElement]
	public float ProportionalGain;

	[XmlElement]
	public float DerivativeGain;

	[XmlElement]
	public float IntegralGain;

	[XmlElement]
	public double ProcessValue;

	[XmlElement]
	public float OutputMaximum = float.MinValue;

	[XmlElement]
	public float OutputMinimum = float.MaxValue;
}
