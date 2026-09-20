using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Voxel;
using ThingImport;
using UnityEngine;

namespace TerrainSystem;

public class MinableVisualiserData : DataCollection
{
	[XmlAttribute("Type")]
	public MinableType MinableType;

	[XmlElement("Mesh")]
	public MeshReference Mesh;

	[XmlElement("Color", typeof(ColorFloat4Reference))]
	[XmlElement("Color32", typeof(Color32Reference))]
	public ColorReference ColorReference = new ColorFloat4Reference(Color.white);

	[XmlElement("TerrainColor", typeof(ColorFloat4Reference))]
	[XmlElement("TerrainColor32", typeof(Color32Reference))]
	public ColorReference TerrainColorReference = new ColorFloat4Reference(Color.white);

	public static Dictionary<MinableType, MinableVisualiserData> MinableVisualizers = new Dictionary<MinableType, MinableVisualiserData>();

	public override bool IsValid()
	{
		if (Mesh != null)
		{
			return Mesh.IsValid();
		}
		return false;
	}

	public override void Initialize(ModAbout mod)
	{
		Mesh?.Load();
		if (IsValid())
		{
			DataCollection.Register(this, mod);
		}
	}

	protected override void OnRegistered()
	{
		base.OnRegistered();
		if (!MinableVisualizers.TryAdd(MinableType, this))
		{
			ConsoleWindow.PrintAction("Override Minable Visualiser Data " + MinableVisualizers[MinableType].Id + " with " + Id);
			MinableVisualizers[MinableType] = this;
		}
	}
}
