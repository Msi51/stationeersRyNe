using System;
using GameEventBus.Events;
using GameEventBus.Interfaces;

namespace GameEventBus;

internal class Subscription<TEventBase> : ISubscription where TEventBase : EventBase
{
	private readonly int _subscriptionToken;

	private readonly Action<TEventBase> _action;

	public int SubscriptionToken => _subscriptionToken;

	public Subscription(Action<TEventBase> action)
	{
		_action = action ?? throw new ArgumentNullException("action");
		_subscriptionToken = action.GetHashCode();
	}

	public void Publish(EventBase eventItem)
	{
		if (!(eventItem is TEventBase))
		{
			throw new ArgumentException("Event Item is not the correct type.");
		}
		_action(eventItem as TEventBase);
	}
}
