using System;
using Assets.Scripts.Networking;

namespace WorldLogSystem;

public static class WorldLogHelper
{
	public static WorldEvent DeserializeWorldEvent(WorldEventType eventType, RocketBinaryReader reader)
	{
		return eventType switch
		{
			WorldEventType.TraderCrashEvent => TraderCrashEvent.Deserialize(reader), 
			WorldEventType.TraderEnteredRangeEvent => TraderEnteredRangeEvent.Deserialize(reader), 
			WorldEventType.TraderLeftRangeEvent => TraderLeftRangeEvent.Deserialize(reader), 
			_ => throw new NullReferenceException("Unable to deserialize world event type"), 
		};
	}
}
