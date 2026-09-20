using System.Xml.Serialization;
using Assets.Scripts.Objects;

[XmlRoot("Objective")]
public class WorldObjectiveSaveData : IReferencableSaveData
{
	[XmlAttribute("ReferenceId")]
	public long ReferenceId;

	[XmlAttribute("Id")]
	public int IdHash;

	[XmlAttribute("Triggered")]
	public bool Triggered;

	[XmlAttribute("Completed")]
	public bool Completed;

	[XmlAttribute("Dismissed")]
	public bool Dismissed;

	public long SavedReferenceId => ReferenceId;

	public WorldObjectiveSaveData()
	{
	}

	public WorldObjectiveSaveData(WorldObjectiveState objective)
	{
		ReferenceId = objective.ReferenceId;
		IdHash = objective.WorldObjective.IdHash;
		Triggered = objective.Triggered;
		Completed = objective.Completed;
		Dismissed = objective.Dismissed;
	}
}
