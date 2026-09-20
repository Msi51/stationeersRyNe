using System;
using System.Xml.Serialization;

[XmlRoot]
public class EnumReference<T> where T : Enum
{
	[XmlAttribute]
	public T Value;

	public EnumReference()
	{
	}

	public EnumReference(T value)
	{
		Value = value;
	}

	public static implicit operator T(EnumReference<T> reference)
	{
		return reference.Value;
	}
}
