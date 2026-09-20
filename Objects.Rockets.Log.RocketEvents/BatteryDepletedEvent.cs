using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;

namespace Objects.Rockets.Log.RocketEvents;

public class BatteryDepletedEvent : RocketEvent
{
	public override RocketEventType RocketEventType => RocketEventType.BatteryDepleted;

	public override string TextColor => "red";

	public override string GetText()
	{
		return GameStrings.RocketLogOutOfBattery.AsString(EventOrigin?.DisplayName ?? string.Empty);
	}

	public BatteryDepletedEvent(IReferencable eventOrigin)
		: base(eventOrigin)
	{
		Hash = (Hash ^ 2) * 41;
	}

	public BatteryDepletedEvent(RocketBinaryReader reader)
		: base(reader)
	{
	}
}
