using Assets.Scripts.Objects;

namespace Assets.Scripts.Networking;

public class TrackerMessageFromServer : ProcessedMessage<TrackerMessageFromServer>
{
	public long ThingId;

	public bool ToAdd;

	public override void Process(long hostId)
	{
		if (Thing.Find<Thing>(ThingId) is ITrackable item)
		{
			if (ToAdd)
			{
				ITrackable.Trackables.Add(item);
			}
			else
			{
				ITrackable.Trackables.Remove(item);
			}
		}
	}

	public override void Deserialize(RocketBinaryReader reader)
	{
		ThingId = reader.ReadInt64();
		ToAdd = reader.ReadBoolean();
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteInt64(ThingId);
		writer.WriteBoolean(ToAdd);
	}
}
