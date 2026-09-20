using Assets.Scripts.Networking;

namespace UnityEngine.Networking;

public interface IRocketReaderWriter
{
	void Read(RocketBinaryReader reader);

	void Write(RocketBinaryWriter writer);
}
