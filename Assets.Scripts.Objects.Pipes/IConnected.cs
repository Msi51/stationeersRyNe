using Assets.Scripts.Networks;

namespace Assets.Scripts.Objects.Pipes;

public interface IConnected
{
	CableNetwork GetNetwork(int networkIndex);
}
