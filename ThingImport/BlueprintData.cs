using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;

namespace ThingImport;

public class BlueprintData : DataCollection
{
	[XmlElement("Mesh")]
	public MeshReference MeshRef;

	[XmlElement("Edge")]
	public List<EdgeData> Edges = new List<EdgeData>();

	public override void Initialize(ModAbout mod)
	{
		MeshRef?.Load();
		foreach (EdgeData edge in Edges)
		{
			edge.Initialize();
		}
		DataCollection.Register(this, mod);
	}
}
