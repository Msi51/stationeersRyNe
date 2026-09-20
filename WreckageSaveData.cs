using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

public class WreckageSaveData : StackableSaveData
{
	[XmlElement]
	public int WreckedParentPrefabHash;
}
