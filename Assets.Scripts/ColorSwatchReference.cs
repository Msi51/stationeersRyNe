using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts;

public class ColorSwatchReference : IChecksum
{
	private const string ID_ATTRIBUTE = "Id";

	[XmlAttribute("Id")]
	public string ColorId;

	[XmlIgnore]
	private ColorSwatch _colorSwatch;

	[XmlIgnore]
	private int _colorHash;

	public void Initialize()
	{
		_colorHash = Animator.StringToHash(ColorId);
		string colorName = ((!ColorId.Contains("Color")) ? ("Color" + ColorId) : ColorId);
		_colorSwatch = GameManager.GetColorSwatch(colorName);
	}

	public int GetChecksum()
	{
		return _colorHash;
	}

	public static implicit operator ColorSwatch(ColorSwatchReference colorSwatchReference)
	{
		return colorSwatchReference._colorSwatch;
	}

	public ColorSwatchReference()
	{
	}

	public ColorSwatchReference(ColorSwatch colorSwatch)
	{
		ColorId = colorSwatch.Name;
	}

	public int GetIndex()
	{
		return _colorSwatch.Index;
	}

	public void Add(ref XElement parent, string elementName)
	{
		if (!string.IsNullOrEmpty(ColorId))
		{
			XElement xElement = new XElement(elementName);
			XDocumentHelper.SetAttribute(xElement, "Id", ColorId);
			parent.Add(xElement);
		}
	}
}
