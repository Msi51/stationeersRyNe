namespace WorldLogSystem;

public class TraderEnteredRangeEventData : WorldEventData
{
	public override WorldEvent ToEvent()
	{
		return new TraderEnteredRangeEvent
		{
			Text = Text,
			DateTime = DateTime
		};
	}
}
