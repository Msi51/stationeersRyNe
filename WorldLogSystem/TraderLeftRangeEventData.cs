namespace WorldLogSystem;

public class TraderLeftRangeEventData : WorldEventData
{
	public override WorldEvent ToEvent()
	{
		return new TraderLeftRangeEvent
		{
			Text = Text,
			DateTime = DateTime
		};
	}
}
