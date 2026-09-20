using System.Xml.Serialization;
using Assets.Scripts.Objects.Electrical;

namespace Objects.Electrical;

[XmlInclude(typeof(CableSaveSaveData))]
public class PowerPylonCableTerminusSaveData : CableSaveSaveData
{
}
