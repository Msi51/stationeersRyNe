using System.Xml.Serialization;
using Assets.Scripts.Objects;
using Trading;

namespace Assets.Scripts;

[XmlType("PendingSpawnAction")]
public class PendingSpawnActionSaveData
{
	[XmlAttribute("Thing")]
	public long ThingReferenceId;

	[XmlAttribute("Player")]
	public long PlayerReferenceId;

	[XmlElement("Action")]
	public DelayedAction Action;

	[XmlElement("TimeRemaining")]
	public float TimeRemaining;

	[XmlIgnore]
	public Thing Thing { get; private set; }

	[XmlIgnore]
	public Entity Player { get; private set; }

	public PendingSpawnActionSaveData()
	{
	}

	public PendingSpawnActionSaveData(Thing thing, Entity player, DelayedAction action, float timeRemaining)
	{
		Thing = thing;
		Player = player;
		ThingReferenceId = thing.ReferenceId;
		PlayerReferenceId = player?.ReferenceId ?? 0;
		Action = action;
		TimeRemaining = timeRemaining;
	}

	public bool Validate()
	{
		Player = Thing.Find<Entity>(PlayerReferenceId);
		Thing = Thing.Find<DynamicThing>(ThingReferenceId);
		if (Player != null)
		{
			return Thing != null;
		}
		return false;
	}
}
