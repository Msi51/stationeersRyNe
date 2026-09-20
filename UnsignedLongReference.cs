using System.Xml.Serialization;

public class UnsignedLongReference
{
	[XmlAttribute("Value")]
	public ulong Value;

	public UnsignedLongReference()
	{
	}

	public UnsignedLongReference(ulong value)
	{
		Value = value;
	}

	public static implicit operator ulong(UnsignedLongReference unsignedLongReference)
	{
		return unsignedLongReference.Value;
	}
}
