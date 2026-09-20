using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using ThingImport;
using UnityEngine;

namespace Objects.Rockets;

public class NodeIcon
{
	[XmlAttribute("Id")]
	public string Id;

	[XmlElement("Path")]
	public string Path;

	[XmlAttribute("Size")]
	public float Size = 1f;

	[XmlAttribute("Tint")]
	public string Tint;

	[XmlAttribute("SelectionSize")]
	public float SelectionSize = 1f;

	[XmlIgnore]
	public int IdHash;

	[XmlIgnore]
	public Texture2D Icon;

	[XmlIgnore]
	public Sprite IconSprite;

	[XmlIgnore]
	public Color TintColor;

	public static Dictionary<int, NodeIcon> IconLookup = new Dictionary<int, NodeIcon>();

	public void Initialise()
	{
		Texture2D texture2D = Find(Id)?.Icon;
		if ((object)texture2D == null && !string.IsNullOrEmpty(Path))
		{
			texture2D = StreamingAssetLoader.LoadTextureFromStreamingAssets(Path, TextureFormat.DXT5);
			Register(this, texture2D);
		}
		if ((object)texture2D == null)
		{
			ConsoleWindow.PrintError("Initialise SpaceMap Error! Unable to find Texture for Icon " + Id);
		}
		IdHash = Animator.StringToHash(Id);
		Icon = texture2D;
		IconSprite = Sprite.Create(Icon, new Rect(0f, 0f, Icon.width, Icon.height), Vector2.one * 0.5f);
		if (Size == 0f)
		{
			Size = 1f;
		}
		SetTintColor();
	}

	private void SetTintColor()
	{
		TintColor = Color.white;
		if (!string.IsNullOrWhiteSpace(Tint))
		{
			string[] array = Tint.Split(",", StringSplitOptions.RemoveEmptyEntries);
			if (array.Length == 4 && float.TryParse(array[0], out var result) && float.TryParse(array[1], out var result2) && float.TryParse(array[2], out var result3) && float.TryParse(array[3], out var result4))
			{
				TintColor = new Color(result / 255f, result2 / 255f, result3 / 255f, result4 / 255f);
			}
		}
	}

	public static NodeIcon Find(string name)
	{
		return Find(Animator.StringToHash(name));
	}

	public static NodeIcon Find(int id)
	{
		IconLookup.TryGetValue(id, out var value);
		return value;
	}

	public static void Register(NodeIcon nodeIcon, Texture2D icon)
	{
		nodeIcon.IdHash = Animator.StringToHash(nodeIcon.Id);
		nodeIcon.Icon = icon;
		IconLookup.Add(nodeIcon.IdHash, nodeIcon);
	}
}
