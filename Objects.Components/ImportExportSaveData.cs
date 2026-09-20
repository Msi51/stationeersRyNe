using System.Xml.Serialization;
using Assets.Scripts.Objects;

namespace Objects.Components;

[XmlInclude(typeof(StructureSaveData))]
public class ImportExportSaveData : StructureSaveData
{
}
