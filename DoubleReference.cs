using System.Xml.Serialization;

[XmlRoot]
public class DoubleReference
{
	[XmlAttribute]
	public double Value;

	public DoubleReference()
	{
	}

	public DoubleReference(double value)
	{
		Value = value;
	}
}
