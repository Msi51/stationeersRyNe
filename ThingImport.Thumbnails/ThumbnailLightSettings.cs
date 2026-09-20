using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Serialization;

namespace ThingImport.Thumbnails;

[XmlRoot("ThumbnailLights")]
public class ThumbnailLightSettings
{
	public const int CurrentVersion = 2;

	[XmlAttribute]
	public int Version;

	[XmlAttribute]
	public float Saturation = 1f;

	[XmlArray("Lights")]
	[XmlArrayItem("Light")]
	public List<ThumbnailLightSetting> Lights = new List<ThumbnailLightSetting>();

	public static ThumbnailLightSettings Load()
	{
		ThumbnailLightSettings thumbnailLightSettings = XmlSerialization.LoadOrNull<ThumbnailLightSettings>(ThumbnailPaths.ForRead(ThumbnailPaths.LightsFile));
		if (thumbnailLightSettings == null || thumbnailLightSettings.Version != 2)
		{
			return new ThumbnailLightSettings
			{
				Version = 2
			};
		}
		return thumbnailLightSettings;
	}

	public void Save()
	{
		Version = 2;
		this.SaveXml(ThumbnailPaths.LightsFile);
	}

	public ThumbnailLightSetting Find(string name)
	{
		return Lights.Find((ThumbnailLightSetting l) => l != null && l.Name == name);
	}

	public void AddOrUpdate(string name, bool enabled, float intensity, float pitch, float yaw, float roll)
	{
		ThumbnailLightSetting thumbnailLightSetting = Find(name);
		if (thumbnailLightSetting == null)
		{
			thumbnailLightSetting = new ThumbnailLightSetting
			{
				Name = name
			};
			Lights.Add(thumbnailLightSetting);
		}
		thumbnailLightSetting.Enabled = enabled;
		thumbnailLightSetting.Intensity = intensity;
		thumbnailLightSetting.Pitch = pitch;
		thumbnailLightSetting.Yaw = yaw;
		thumbnailLightSetting.Roll = roll;
	}
}
