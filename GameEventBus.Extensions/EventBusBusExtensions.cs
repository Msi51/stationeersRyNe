using System;
using GameEventBus.Events;
using GameEventBus.Interfaces;

namespace GameEventBus.Extensions;

public static class EventBusBusExtensions
{
	public static void Publish(this EventBase eventBase, IEventBus eventBus)
	{
		eventBus.Publish(eventBase);
	}

	public static void PublishAsync(this EventBase eventBase, IEventBus eventBus)
	{
		eventBus.PublishAsync(eventBase);
	}

	public static void PublishAsync(this EventBase eventBase, IEventBus eventBus, AsyncCallback asyncCallback)
	{
		eventBus.PublishAsync(eventBase, asyncCallback);
	}
}
