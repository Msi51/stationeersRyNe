using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(DynamicThingSaveData))]
public class GasCanisterSaveData : DynamicThingSaveData
{
	public bool HasBlown;
}
