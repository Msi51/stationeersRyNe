using System;
using GameEventBus.Events;

namespace GameEventBus.Interfaces;

public interface IEventBus
{
	void Subscribe<TEventBase>(Action<TEventBase> action) where TEventBase : EventBase;

	void Unsubscribe<TEventBase>(Action<TEventBase> token) where TEventBase : EventBase;

	void Publish<TEventBase>(TEventBase eventItem) where TEventBase : EventBase;

	void PublishAsync<TEventBase>(TEventBase eventItem) where TEventBase : EventBase;

	void PublishAsync<TEventBase>(TEventBase eventItem, AsyncCallback callback) where TEventBase : EventBase;
}
