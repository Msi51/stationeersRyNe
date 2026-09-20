using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Items;

[XmlInclude(typeof(StructureSaveData))]
public class GasMaskSaveData : AtmosphericItemSaveData
{
}
