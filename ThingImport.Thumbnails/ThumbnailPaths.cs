using System.IO;
using Assets.Scripts.Objects;
using UnityEngine;

namespace ThingImport.Thumbnails;

public static class ThumbnailPaths
{
	private static string _pngFolder;

	private static string _dataFolder;

	private static bool _isProjectFolder;

	public static string Folder
	{
		get
		{
			if (_pngFolder != null)
			{
				return _pngFolder;
			}
			string text = Path.Combine(Application.dataPath, "Resources", "UI", "Thumbnails");
			if (Directory.Exists(text))
			{
				_isProjectFolder = true;
				_pngFolder = text;
				return _pngFolder;
			}
			_pngFolder = SaveDataFolder;
			Directory.CreateDirectory(_pngFolder);
			return _pngFolder;
		}
	}

	public static bool IsProjectFolder
	{
		get
		{
			_ = Folder;
			return _isProjectFolder;
		}
	}

	public static string DataFolder
	{
		get
		{
			if (_dataFolder != null)
			{
				return _dataFolder;
			}
			_dataFolder = (IsProjectFolder ? Path.Combine(Application.dataPath, "Data", "Thumbnails") : SaveDataFolder);
			Directory.CreateDirectory(_dataFolder);
			return _dataFolder;
		}
	}

	private static string SaveDataFolder => Path.Combine(StationSaveUtils.GetSavePath(), "thumbnails");

	public static string ExportFolder
	{
		get
		{
			string text = Path.Combine(StationSaveUtils.GetSavePath(), "renders");
			Directory.CreateDirectory(text);
			return text;
		}
	}

	public static string LinksFile => Path.Combine(DataFolder, "thumbnail_links.xml");

	public static string RotationsFile => Path.Combine(DataFolder, "thumbnail_rotations.xml");

	public static string ItemStatesFile => Path.Combine(DataFolder, "thumbnail_items.xml");

	public static string LightsFile => Path.Combine(DataFolder, "thumbnail_lights.xml");

	public static string ForRead(string path)
	{
		if (File.Exists(path))
		{
			return path;
		}
		string text = Path.Combine(SaveDataFolder, Path.GetFileName(path));
		if (!File.Exists(text))
		{
			return path;
		}
		return text;
	}

	public static string ColorVariantName(ColorSwatch swatch, int index)
	{
		if (swatch?.Normal != null)
		{
			return swatch.Normal.name.Replace("Color", "");
		}
		if (string.IsNullOrEmpty(swatch?.Name))
		{
			return $"Color{index}";
		}
		return swatch.Name;
	}

	public static string Base(string prefabName)
	{
		return Path.Combine(Folder, Sanitize(prefabName) + ".png");
	}

	public static string Variant(string prefabName, string colorName)
	{
		return Path.Combine(Folder, Sanitize(prefabName) + "_" + Sanitize(colorName) + ".png");
	}

	public static string BuildState(string prefabName, int index)
	{
		return Path.Combine(Folder, $"{Sanitize(prefabName)}_BuildState{index}.png");
	}

	private static string Sanitize(string name)
	{
		return name?.Replace(" ", "") ?? "Unnamed";
	}
}
