using System.Xml.Serialization;

namespace Assets.Scripts;

public enum SpawnEvent
{
	[XmlEnum("None")]
	None,
	[XmlEnum("NewWorld")]
	NewWorld,
	[XmlEnum("NewPlayerKit")]
	NewPlayerKit,
	[XmlEnum("RespawnPlayerKit")]
	RespawnPlayerKit,
	[XmlEnum("NewPlayer")]
	NewPlayer,
	[XmlEnum("RespawnPlayer")]
	RespawnPlayer
}
