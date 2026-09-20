namespace Assets.Scripts.Networking;

public enum HandshakeType : byte
{
	None,
	ClientReady,
	Disconnecting,
	Rejected
}
