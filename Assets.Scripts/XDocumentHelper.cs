using System.Xml.Linq;
using UnityEngine;

namespace Assets.Scripts;

public static class XDocumentHelper
{
	public static void SetOrRemoveAttribute(XElement element, bool useAttribute, string attributeName, string attributeValue)
	{
		if (useAttribute)
		{
			SetAttribute(element, attributeName, attributeValue);
		}
		else
		{
			RemoveAttribute(element, attributeName);
		}
	}

	public static void SetAttribute(XElement element, string attributeName, string attributeValue)
	{
		if (attributeValue != null)
		{
			element.SetAttributeValue(attributeName, attributeValue);
		}
	}

	public static void SetValue(XElement element, string value)
	{
		element.SetValue(value);
	}

	public static void RemoveAttribute(XElement element, string attributeName)
	{
		element.Attribute(attributeName)?.Remove();
	}

	public static void MakeAndSetElementUsingAttributes(string name, Vector3 value, ref XElement element)
	{
		XElement xElement = MakeOrGetElement(name, ref element);
		xElement.SetAttributeValue("x", value.x.ToString("F4"));
		xElement.SetAttributeValue("y", value.y.ToString("F4"));
		xElement.SetAttributeValue("z", value.z.ToString("F4"));
	}

	public static XElement MakeOrGetElement(string elementName, ref XElement parentElement)
	{
		return parentElement.Element(elementName) ?? MakeElement(elementName, ref parentElement);
	}

	public static XElement MakeElement(string elementName, ref XElement parentElement)
	{
		XElement xElement = new XElement(elementName);
		parentElement.Add(xElement);
		return xElement;
	}
}
