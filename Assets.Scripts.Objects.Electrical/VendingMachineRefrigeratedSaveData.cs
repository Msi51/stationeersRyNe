using System.Xml.Serialization;

namespace Assets.Scripts.Objects.Electrical;

[XmlInclude(typeof(VendingMachineRefrigeratedSaveData))]
public class VendingMachineRefrigeratedSaveData : VendingMachineSaveData
{
	[XmlElement]
	public double Setting;
}
