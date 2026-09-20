using System.Xml.Serialization;

[XmlRoot]
public class BoolReference
{
	[XmlAttribute]
	public bool Value;

	public BoolReference()
	{
	}

	public BoolReference(bool value)
	{
		Value = value;
	}

	public static implicit operator bool(BoolReference reference)
	{
		return reference.Value;
	}
}
