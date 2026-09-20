using Assets.Scripts.Networking;

namespace Objects.Rockets.Mining;

public interface INetworkNullable
{
	void Write(RocketBinaryWriter writer);
}
