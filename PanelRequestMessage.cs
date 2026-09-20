using GameEventBus.Events;

public class PanelRequestMessage : EventBase
{
	public string PanelType;

	public bool Active;

	public PanelRequestMessage(string panelType, bool active)
	{
		PanelType = panelType;
		Active = active;
	}
}
