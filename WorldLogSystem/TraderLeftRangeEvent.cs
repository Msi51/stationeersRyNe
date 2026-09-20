using Assets.Scripts.Networking;

namespace WorldLogSystem;

public class TraderLeftRangeEvent : WorldEvent
{
	public override string TextColor => "white";

	public override WorldEventType WorldEventType => WorldEventType.TraderLeftRangeEvent;

	public TraderLeftRangeEvent()
	{
	}

	public TraderLeftRangeEvent(string text)
	{
		Text = text;
		DateTime = base.DateTimeNow;
	}

	public override WorldEventData ToData()
	{
		return new TraderLeftRangeEventData
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

	public static TraderLeftRangeEvent Deserialize(RocketBinaryReader reader)
	{
		return new TraderLeftRangeEvent
		{
			DateTime = reader.ReadString(),
			Text = reader.ReadString()
		};
	}
}
