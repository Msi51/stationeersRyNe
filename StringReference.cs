using System;
using System.Xml.Serialization;

[Serializable]
public class StringReference : IChecksum
{
	protected const string VALUE_ATTRIBUTE = "Value";

	[XmlAttribute("Value")]
	public string Value;

	public StringReference()
	{
	}

	public StringReference(string value)
	{
		Value = value;
	}

	private string GetBasicString()
	{
		return Value;
	}

	public override string ToString()
	{
		return Value;
	}

	public int GetChecksum()
	{
		return 0;
	}

	public static implicit operator string(StringReference code)
	{
		return code?.GetBasicString() ?? string.Empty;
	}
}
