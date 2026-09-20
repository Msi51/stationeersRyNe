using System;
using Assets.Scripts.Networking;

namespace WorldLogSystem;

public abstract class WorldEvent
{
	public string Text;

	public string DateTime;

	public abstract string TextColor { get; }

	public abstract WorldEventType WorldEventType { get; }

	protected string DateTimeNow => System.DateTime.Now.ToString("hh:mm dd/MM/yyyy");

	public abstract WorldEventData ToData();

	public abstract void Serialize(RocketBinaryWriter writer);
}
