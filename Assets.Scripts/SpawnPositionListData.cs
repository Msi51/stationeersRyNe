using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts;

public class SpawnPositionListData
{
	[XmlAttribute("Id")]
	public string PrefabId;

	[XmlAttribute("Count")]
	public int SpawnCount;

	[XmlElement("Transform")]
	public List<TransformData> Transforms;

	[XmlIgnore]
	public Thing Prefab { get; set; }

	public void Initialise()
	{
		Prefab = Assets.Scripts.Objects.Prefab.Find<Thing>(PrefabId);
		if ((object)Prefab == null && WorldManager.Instance != null)
		{
			ConsoleWindow.PrintError("Spawn Prefab " + PrefabId + " is not a valid prefab.");
		}
	}

	public void Execute()
	{
		Transforms.Sort((TransformData a, TransformData b) => Random.Range(-1, 2));
		int num = Mathf.Min(SpawnCount, Transforms.Count);
		for (int num2 = 0; num2 < num; num2++)
		{
			TransformData transformData = Transforms[num2];
			Thing.Create<Thing>(Prefab, transformData.Position(), transformData.Rotation(), 0L);
		}
	}
}
