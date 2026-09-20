using System.Xml.Serialization;
using Assets.Scripts.Networks;

namespace Assets.Scripts.Objects.Pipes;

[XmlInclude(typeof(StructureSaveData))]
public class PipeSaveData : StructureSaveData
{
	[XmlElement]
	public long PipeNetworkId;

	[XmlElement]
	public PipeBurst IsBurst;

	[XmlElement]
	public PipeBurst DamageRecord;

	public override bool IsValidData()
	{
		return base.IsValidData();
	}
}
