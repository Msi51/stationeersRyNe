using Assets.Scripts.Networking;

namespace WorldLogSystem;

public class TraderEnteredRangeEvent : WorldEvent
{
	public override string TextColor => "white";

	public override WorldEventType WorldEventType => WorldEventType.TraderEnteredRangeEvent;

	public TraderEnteredRangeEvent()
	{
	}

	public TraderEnteredRangeEvent(string text)
	{
		Text = text;
		DateTime = base.DateTimeNow;
	}

	public override WorldEventData ToData()
	{
		return new TraderEnteredRangeEventData
		{
			Text = Text,
			DateTime = DateTime
		};
	}

	public override void Serialize(RocketBinaryWriter writer)
	{
		writer.WriteString(DateTime);
		writer.WriteString(Text);
	}

	public static TraderEnteredRangeEvent Deserialize(RocketBinaryReader reader)
	{
		return new TraderEnteredRangeEvent
		{
			DateTime = reader.ReadString(),
			Text = reader.ReadString()
		};
	}
}
