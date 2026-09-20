using System;
using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Util;

[Serializable]
public class LocalizedStringReference : StringReference
{
	private const string KEY_ATTRIBUTE = "Key";

	[XmlAttribute("Key")]
	public string Key;

	private const string COLOR_ATTRIBUTE = "Color";

	[XmlAttribute("Color")]
	public string Color;

	public LocalizedStringReference()
	{
	}

	public LocalizedStringReference(string value)
	{
		Value = value;
	}

	public static implicit operator string(LocalizedStringReference code)
	{
		return code?.GetBasicString() ?? string.Empty;
	}

	private string GetBasicString()
	{
		if (string.IsNullOrEmpty(Key))
		{
			return Value;
		}
		return Localization.GetInterface(Key);
	}

	public override string ToString()
	{
		if (string.IsNullOrEmpty(Color))
		{
			return GetBasicString();
		}
		return GetBasicString().AsColor(Color.ToLower());
	}

	public string AsColor(string color)
	{
		if (string.IsNullOrEmpty(color))
		{
			return GetBasicString();
		}
		return GetBasicString().AsColor(color.ToLower());
	}

	public void Add(ref XElement parentElement, string elementname)
	{
		if (!string.IsNullOrEmpty(Key) || !string.IsNullOrEmpty(Value))
		{
			XElement xElement = XDocumentHelper.MakeElement(elementname, ref parentElement);
			XDocumentHelper.SetAttribute(xElement, "Key", Key);
			XDocumentHelper.SetAttribute(xElement, "Value", Value);
			XDocumentHelper.SetAttribute(xElement, "Color", Color);
			parentElement.Add(xElement);
		}
	}
}
