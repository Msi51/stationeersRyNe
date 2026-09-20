using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Serialization;

namespace ThingImport.Thumbnails;

[XmlRoot("ThumbnailLinks")]
public class ThumbnailLinkData
{
	[XmlArray("Groups")]
	[XmlArrayItem("Group")]
	public List<ThumbnailLinkGroup> Groups = new List<ThumbnailLinkGroup>();

	public static ThumbnailLinkData Load()
	{
		return XmlSerialization.LoadOrNull<ThumbnailLinkData>(ThumbnailPaths.ForRead(ThumbnailPaths.LinksFile)) ?? new ThumbnailLinkData();
	}

	public void Save()
	{
		this.SaveXml(ThumbnailPaths.LinksFile);
	}

	public ThumbnailLinkGroup GroupFor(string prefabName)
	{
		return Groups.Find((ThumbnailLinkGroup g) => g?.Members.Contains(prefabName) ?? false);
	}

	public ThumbnailLinkGroup Link(string leader, IEnumerable<string> members)
	{
		List<string> list = new List<string> { leader };
		foreach (string member in members)
		{
			if (!string.IsNullOrEmpty(member) && !list.Contains(member))
			{
				list.Add(member);
			}
		}
		foreach (string item in list)
		{
			RemoveFromGroup(item);
		}
		ThumbnailLinkGroup thumbnailLinkGroup = new ThumbnailLinkGroup
		{
			Leader = leader,
			Members = list
		};
		Groups.Add(thumbnailLinkGroup);
		return thumbnailLinkGroup;
	}

	public void Unlink(string prefabName)
	{
		RemoveFromGroup(prefabName);
	}

	private void RemoveFromGroup(string prefabName)
	{
		ThumbnailLinkGroup thumbnailLinkGroup = GroupFor(prefabName);
		if (thumbnailLinkGroup != null)
		{
			thumbnailLinkGroup.Members.Remove(prefabName);
			if (thumbnailLinkGroup.Members.Count < 2)
			{
				Groups.Remove(thumbnailLinkGroup);
			}
			else if (thumbnailLinkGroup.Leader == prefabName)
			{
				thumbnailLinkGroup.Leader = thumbnailLinkGroup.Members[0];
			}
		}
	}
}
