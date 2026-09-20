using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace ThingImport.Thumbnails;

[XmlRoot("ThumbnailRotations")]
public class ThumbnailRotationPresets
{
	[XmlArray("Rotations")]
	[XmlArrayItem("Rotation")]
	public List<ThumbnailRotationPreset> Presets = new List<ThumbnailRotationPreset>();

	public static ThumbnailRotationPresets Load()
	{
		return XmlSerialization.LoadOrNull<ThumbnailRotationPresets>(ThumbnailPaths.ForRead(ThumbnailPaths.RotationsFile)) ?? new ThumbnailRotationPresets();
	}

	public void Save()
	{
		this.SaveXml(ThumbnailPaths.RotationsFile);
	}

	public void AddOrUpdate(string name, Vector3 euler, float zoom)
	{
		ThumbnailRotationPreset thumbnailRotationPreset = Presets.Find((ThumbnailRotationPreset p) => p != null && p.Name == name);
		if (thumbnailRotationPreset == null)
		{
			thumbnailRotationPreset = new ThumbnailRotationPreset
			{
				Name = name
			};
			Presets.Add(thumbnailRotationPreset);
		}
		thumbnailRotationPreset.X = euler.x;
		thumbnailRotationPreset.Y = euler.y;
		thumbnailRotationPreset.Z = euler.z;
		thumbnailRotationPreset.Zoom = zoom;
	}

	public void Remove(string name)
	{
		Presets.RemoveAll((ThumbnailRotationPreset p) => p == null || p.Name == name);
	}
}
