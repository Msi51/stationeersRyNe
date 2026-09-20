using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Serialization;
using UnityEngine;

namespace ThingImport.Thumbnails;

[XmlRoot("ThumbnailItemStates")]
public class ThumbnailItemStates
{
	[XmlArray("Items")]
	[XmlArrayItem("Item")]
	public List<ThumbnailItemState> Items = new List<ThumbnailItemState>();

	public static ThumbnailItemStates Load()
	{
		return XmlSerialization.LoadOrNull<ThumbnailItemStates>(ThumbnailPaths.ForRead(ThumbnailPaths.ItemStatesFile)) ?? new ThumbnailItemStates();
	}

	public void Save()
	{
		this.SaveXml(ThumbnailPaths.ItemStatesFile);
	}

	public ThumbnailItemState Find(string prefabName)
	{
		return Items.Find((ThumbnailItemState i) => i != null && i.Name == prefabName);
	}

	public void AddOrUpdate(string prefabName, Vector3 euler, float zoom, string clip, float clipTime, string states)
	{
		ThumbnailItemState thumbnailItemState = Find(prefabName);
		if (thumbnailItemState == null)
		{
			thumbnailItemState = new ThumbnailItemState
			{
				Name = prefabName
			};
			Items.Add(thumbnailItemState);
		}
		thumbnailItemState.X = euler.x;
		thumbnailItemState.Y = euler.y;
		thumbnailItemState.Z = euler.z;
		thumbnailItemState.Zoom = zoom;
		thumbnailItemState.Clip = clip;
		thumbnailItemState.ClipTime = clipTime;
		thumbnailItemState.States = states;
	}
}
