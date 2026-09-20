using System;
using System.IO;
using System.Xml.Serialization;
using Assets.Scripts.Networking.Transports;
using Assets.Scripts.Serialization;
using Newtonsoft.Json.Linq;
using UnityEngine;

public abstract class ModData
{
	[XmlAttribute("Enabled")]
	public bool Enabled = true;

	[XmlElement("Path")]
	public PathReference DirectoryPath = new PathReference();

	[XmlIgnore]
	private ModAbout _modAboutData;

	[XmlIgnore]
	private Texture2D _previewTexture;

	[Obsolete("Use 'Enabled' instead")]
	public bool IsEnabled => Enabled;

	[Obsolete("Use 'this is CoreModData' instead")]
	public bool IsCore => this is CoreModData;

	[Obsolete("Use 'DirectoryPath' instead")]
	public string LocalPath => DirectoryPath;

	public string AboutPath => Path.Combine(DirectoryPath, "About");

	public string AboutXmlPath => Path.Combine(AboutPath, "About.xml");

	public ModData()
	{
	}

	public override string ToString()
	{
		return JObject.FromObject(this).ToString();
	}

	public ModAbout GetAboutData()
	{
		if (_modAboutData != null)
		{
			return _modAboutData;
		}
		if (this is CoreModData)
		{
			_modAboutData = new ModAbout
			{
				Name = new string("Core"),
				Author = new string("Rocketwerkz"),
				Description = new string("Default settings for all game values. Can be reordered but not disabled."),
				Version = new string("0.1"),
				IsValid = true
			};
		}
		else if (File.Exists(AboutXmlPath))
		{
			_modAboutData = XmlSerialization.Deserialize<ModAbout>(AboutXmlPath, "ModMetadata") ?? GetFailedAbout();
		}
		else
		{
			_modAboutData = GetFailedAbout();
		}
		return _modAboutData;
	}

	private ModAbout GetFailedAbout()
	{
		return new ModAbout
		{
			IsValid = false,
			Name = "Unknown",
			Description = new StringReference("Failed to find About.xml.\nExpected it to be here: " + AboutXmlPath)
		};
	}

	public Texture2D GetPreviewImage()
	{
		if ((bool)_previewTexture)
		{
			return _previewTexture;
		}
		if (this is CoreModData)
		{
			_previewTexture = Resources.Load<Texture2D>("UI/StationeersBanner");
			return _previewTexture;
		}
		try
		{
			string path = Path.Combine(AboutPath, "Preview.png");
			if (File.Exists(path))
			{
				_previewTexture = new Texture2D(2, 2);
				_previewTexture.LoadImage(File.ReadAllBytes(path));
				return _previewTexture;
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		return null;
	}

	public virtual DirectoryInfo GameDataFolder()
	{
		return new DirectoryInfo(Path.Combine(DirectoryPath, "GameData"));
	}

	public static ModData CreateFrom(SteamTransport.ItemWrapper item)
	{
		if (item.Id > 1)
		{
			return new WorkshopModData(item, isEnabled: true);
		}
		return new LocalModData(item.DirectoryPath, enabled: true);
	}
}
