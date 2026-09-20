namespace Assets.Scripts.Networking;

public enum NetworkChannel
{
	GeneralTraffic = 134,
	PlayerJoin,
	StateTick,
	Unreliable,
	SteamP2PConnectionRequest,
	SteamP2PConnectionAccepted,
	SteamP2PHeartbeat,
	PhysicsTick,
	NumChannels
}
