using System.Xml.Serialization;

[XmlRoot]
public class IntReference
{
	[XmlAttribute]
	public int Value;

	public IntReference()
	{
	}

	public IntReference(int value)
	{
		Value = value;
	}

	public static implicit operator int(IntReference reference)
	{
		return reference.Value;
	}
}
