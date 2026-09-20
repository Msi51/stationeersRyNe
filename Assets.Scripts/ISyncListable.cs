using Assets.Scripts.Networking;

namespace Assets.Scripts;

public interface ISyncListable
{
	void Serialize(RocketBinaryWriter writer);
}
