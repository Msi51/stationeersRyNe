using System.Xml.Serialization;
using CharacterCustomisation;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(DynamicThingSaveData))]
public class BrainSaveData : DynamicThingSaveData
{
	[XmlElement]
	public ulong ClientSteamId;

	[XmlElement]
	public PlayerCosmetics identity;
}
