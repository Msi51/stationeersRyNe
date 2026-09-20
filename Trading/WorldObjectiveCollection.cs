using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts;

namespace Trading;

public class WorldObjectiveCollection : DataCollection
{
	[XmlElement("Objective")]
	public List<WorldObjective> WorldObjectives = new List<WorldObjective>();

	public override bool IsValid()
	{
		return WorldObjectives.Count > 0;
	}

	public List<WorldObjective> GetObjectives()
	{
		return DataCollection.Get<WorldObjectiveCollection>(Id).WorldObjectives;
	}

	public string GetName()
	{
		return DataCollection.Get<WorldObjectiveCollection>(Id).Name;
	}

	public override void Initialize(ModAbout mod)
	{
		if (!IsValid())
		{
			return;
		}
		foreach (WorldObjective worldObjective in WorldObjectives)
		{
			worldObjective.Initialize(mod);
		}
		DataCollection.Register(this, mod);
	}
}
