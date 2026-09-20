using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Items;

[XmlInclude(typeof(DynamicThingSaveData))]
public class VoxelToolSaveData : DynamicThingSaveData
{
	[XmlElement]
	public float CurrentPower;
}
