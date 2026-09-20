using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using ThingImport;
using UI.ImGuiUi;

namespace UnityEngine;

[XmlRoot("RegionSet")]
public class RegionSet : DataCollection, IComboable
{
	[XmlIgnore]
	public int TextureHeight;

	[XmlIgnore]
	public int TextureWidth;

	[XmlIgnore]
	private Color[] _data;

	[XmlElement("Region")]
	public List<Region> Regions = new List<Region>();

	[XmlIgnore]
	public Dictionary<Vector3Int, Region> RegionLookup = new Dictionary<Vector3Int, Region>();

	[XmlElement("Texture")]
	public TextureReference TextureReference { get; set; }

	public override void Initialize(ModAbout mod)
	{
		if (!IsValid())
		{
			return;
		}
		TextureReference.Load();
		_data = TextureReference?.Texture?.GetPixels() ?? Array.Empty<Color>();
		TextureWidth = (TextureReference?.Texture?.width).GetValueOrDefault();
		TextureHeight = (TextureReference?.Texture?.height).GetValueOrDefault();
		RegionLookup.Clear();
		foreach (Region region in Regions)
		{
			region.Initialize(mod);
			Vector3Int key = new Vector3Int(region.R, region.G, region.B);
			RegionLookup.Add(key, region);
		}
		DataCollection.Register(this, mod);
	}

	public override bool IsValid()
	{
		return TextureReference != null;
	}

	public bool TryGetRegionFromColor(Color color, out Region region)
	{
		int x = Mathf.RoundToInt(color.r * 255f);
		int y = Mathf.RoundToInt(color.g * 255f);
		int z = Mathf.RoundToInt(color.b * 255f);
		Vector3Int key = new Vector3Int(x, y, z);
		return RegionLookup.TryGetValue(key, out region);
	}

	public string GetKey()
	{
		return Name?.Key ?? Id;
	}

	public string GetName()
	{
		return Name?.ToString() ?? Id;
	}

	public Color GetPixel(int x, int y)
	{
		if (_data == null || x < 0 || y < 0 || x >= TextureWidth || y >= TextureHeight)
		{
			return Color.clear;
		}
		int num = y * TextureWidth + x;
		return _data[num];
	}
}
