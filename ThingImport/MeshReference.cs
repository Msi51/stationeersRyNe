using System.Xml.Serialization;
using UnityEngine;

namespace ThingImport;

[XmlRoot]
public class MeshReference
{
	[XmlAttribute("Id")]
	public string Id;

	[XmlAttribute("Path")]
	public string Path;

	[XmlAttribute("Scale")]
	public float Scale = 1f;

	[XmlIgnore]
	public Mesh Mesh;

	public static implicit operator Mesh(MeshReference meshReference)
	{
		return meshReference.Mesh;
	}

	public bool IsValid()
	{
		return !string.IsNullOrEmpty(Path);
	}

	public void Load()
	{
		if (IsValid())
		{
			Mesh = StreamingAssetLoader.LoadMesh(this, Scale);
		}
	}
}
