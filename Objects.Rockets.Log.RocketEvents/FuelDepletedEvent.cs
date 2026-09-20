using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;

namespace Objects.Rockets.Log.RocketEvents;

public class FuelDepletedEvent : RocketEvent
{
	public override RocketEventType RocketEventType => RocketEventType.FuelDepleted;

	public override string TextColor => "red";

	public override string GetText()
	{
		return GameStrings.RocketLogOutOfFuel.AsString(EventOrigin?.DisplayName ?? string.Empty);
	}

	public FuelDepletedEvent(IReferencable eventOrigin)
		: base(eventOrigin)
	{
		Hash = (Hash ^ 1) * 41;
	}

	public FuelDepletedEvent(RocketBinaryReader reader)
		: base(reader)
	{
	}
}
