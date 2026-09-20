namespace Assets.Scripts.Networking;

public interface IMessageProcessable
{
	void Process(long hostId);
}
