using System.Xml.Linq;
using System.Xml.Serialization;
using Assets.Scripts.Util;

namespace Assets.Scripts;

public class SpawnPositionData : IChecksum
{
	private const string RULE_ATTRIBUTE = "Rule";

	[XmlAttribute("Rule")]
	public SpawnPositionRule SpawnPositionRule = SpawnPositionRule.Random;

	private const string OFFSET_ELEMENT = "Offset";

	[XmlElement("Offset")]
	public Vector3Reference Offset;

	private const string ROTATION_ELEMENT = "Rotation";

	[XmlElement("Rotation")]
	public Vector3Reference Rotation;

	public int GetChecksum()
	{
		return (((((((((((((int)SpawnPositionRule * 41) ^ (int)Offset.x) * 41) ^ (int)Offset.y) * 41) ^ (int)Offset.z) * 41) ^ (int)(Rotation.x * 1000f)) * 41) ^ (int)(Rotation.y * 1000f)) * 41) ^ (int)(Rotation.z * 1000f)) * 41;
	}

	public void Add(ref XElement parentElement, string spawnPositionElement)
	{
		XElement parentElement2 = XDocumentHelper.MakeElement(spawnPositionElement, ref parentElement);
		XDocumentHelper.SetAttribute(parentElement2, "Rule", SpawnPositionRule.GetXmlEnumAttributeValueFromEnum());
		Offset?.Add(ref parentElement2, "Offset");
		Rotation?.Add(ref parentElement2, "Rotation");
	}
}
