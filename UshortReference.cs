using System.Xml.Serialization;

[XmlRoot]
public class UshortReference
{
	[XmlAttribute]
	public ushort Value;

	public UshortReference()
	{
	}

	public UshortReference(ushort value)
	{
		Value = value;
	}

	public static implicit operator ushort(UshortReference reference)
	{
		return reference.Value;
	}
}
