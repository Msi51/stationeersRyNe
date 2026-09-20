using System.Xml.Serialization;

public class PathReference
{
	[XmlAttribute("Value")]
	public string Value;

	public PathReference()
	{
	}

	public PathReference(string value)
	{
		Value = value;
	}

	public static implicit operator string(PathReference pathReference)
	{
		return pathReference?.Value ?? string.Empty;
	}

	public override string ToString()
	{
		return Value;
	}
}
