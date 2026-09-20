using GameEventBus.Events;

namespace GameEventBus.Interfaces;

public interface ISubscription
{
	int SubscriptionToken { get; }

	void Publish(EventBase eventBase);
}
