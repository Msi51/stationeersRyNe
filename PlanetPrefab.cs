using System;
using System.Xml.Serialization;
using Assets.Scripts.Networking;
using UnityEngine;

[Serializable]
[XmlRoot]
public class PlanetPrefab
{
	[XmlIgnore]
	public GameObject GameObject;

	public string Name = "";

	public Vector3 Position = Vector3.zero;

	public Vector3 Rotation = Vector3.zero;

	public Vector3 Scale = Vector3.one;

	public void SerializeOnJoin(RocketBinaryWriter writer)
	{
		writer.WriteString(Name);
		writer.WriteVector3(Position);
		writer.WriteVector3(Rotation);
		writer.WriteVector3(Scale);
	}

	public void DeserializeOnJoin(RocketBinaryReader reader)
	{
		Name = reader.ReadString();
		Position = reader.ReadVector3();
		Rotation = reader.ReadVector3();
		Scale = reader.ReadVector3();
	}
}
