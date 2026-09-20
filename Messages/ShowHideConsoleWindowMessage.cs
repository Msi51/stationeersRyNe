using GameEventBus.Events;

namespace Messages;

public class ShowHideConsoleWindowMessage : EventBase
{
	public bool Show;

	public ShowHideConsoleWindowMessage(bool show)
	{
		Show = show;
	}
}
