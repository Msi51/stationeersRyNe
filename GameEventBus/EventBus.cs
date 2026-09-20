using System;
using System.Collections.Generic;
using System.Linq;
using GameEventBus.Events;
using GameEventBus.Interfaces;

namespace GameEventBus;

public class EventBus : IEventBus
{
	private readonly Dictionary<Type, List<ISubscription>> _subscriptions;

	private static readonly object SubscriptionsLock = new object();

	public EventBus()
	{
		_subscriptions = new Dictionary<Type, List<ISubscription>>();
	}

	public void Subscribe<T>(Action<T> action) where T : EventBase
	{
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		lock (SubscriptionsLock)
		{
			if (!_subscriptions.ContainsKey(typeof(T)))
			{
				_subscriptions.Add(typeof(T), new List<ISubscription>());
			}
			_subscriptions[typeof(T)].Add(new Subscription<T>(action));
		}
	}

	public void Unsubscribe<T>(Action<T> action) where T : EventBase
	{
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		lock (SubscriptionsLock)
		{
			Type typeFromHandle = typeof(T);
			if (_subscriptions.ContainsKey(typeFromHandle))
			{
				ISubscription subscription = _subscriptions[typeFromHandle].FirstOrDefault((ISubscription x) => x.SubscriptionToken == action.GetHashCode());
				if (subscription != null)
				{
					_subscriptions[typeFromHandle].Remove(subscription);
				}
			}
		}
	}

	public void Publish<T>(T eventItem) where T : EventBase
	{
		if (eventItem == null)
		{
			throw new ArgumentNullException("eventItem");
		}
		List<ISubscription> list = new List<ISubscription>();
		lock (SubscriptionsLock)
		{
			if (_subscriptions.ContainsKey(typeof(T)))
			{
				list = _subscriptions[typeof(T)];
			}
		}
		foreach (ISubscription item in list)
		{
			item.Publish(eventItem);
		}
	}

	public void PublishAsync<T>(T eventItem) where T : EventBase
	{
		PublishAsyncInternal(eventItem, null);
	}

	public void PublishAsync<T>(T eventItem, AsyncCallback callback) where T : EventBase
	{
		PublishAsyncInternal(eventItem, callback);
	}

	private void PublishAsyncInternal<T>(T eventItem, AsyncCallback callback) where T : EventBase
	{
		((Action)delegate
		{
			Publish(eventItem);
		}).BeginInvoke(callback, null);
	}
}
