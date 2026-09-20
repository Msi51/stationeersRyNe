using Assets.Scripts.Networking;

namespace UnityEngine.Networking;

public interface IMessageSerialisable
{
	void Deserialize(RocketBinaryReader reader);

	void Serialize(RocketBinaryWriter writer);
}
