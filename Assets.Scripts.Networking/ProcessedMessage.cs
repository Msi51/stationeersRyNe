using UnityEngine.Networking;

namespace Assets.Scripts.Networking;

public abstract class ProcessedMessage<T> : MessageBase<T>, IMessageProcessable where T : MessageBase<T>, new()
{
	public virtual void Process(long hostId)
	{
	}
}
