using System.Text;
using System.Text.RegularExpressions;

namespace Assets.Scripts.Objects.Electrical;

public struct AsciiString
{
	public static AsciiString Empty;

	private readonly byte[] _bytes;

	public int Length => _bytes.Length;

	public AsciiString(string text)
	{
		_bytes = Encoding.ASCII.GetBytes(text);
	}

	public AsciiString(byte[] bytes)
	{
		_bytes = bytes;
	}

	public override string ToString()
	{
		return Encoding.ASCII.GetString(_bytes);
	}

	public byte[] GetBytes()
	{
		return _bytes;
	}

	public static string ParseLine(string text, int maxLength)
	{
		text = text.Replace("\t", " ");
		text = Regex.Replace(text, "([^\\x00-\\x7F])", string.Empty);
		if (text.Length > maxLength)
		{
			text = text.Substring(0, maxLength);
		}
		return text;
	}

	public static AsciiString Parse(string sourceCode)
	{
		return new AsciiString(sourceCode);
	}

	public static implicit operator string(AsciiString asciiString)
	{
		return asciiString.ToString();
	}

	static AsciiString()
	{
		Empty = new AsciiString("");
	}
}
