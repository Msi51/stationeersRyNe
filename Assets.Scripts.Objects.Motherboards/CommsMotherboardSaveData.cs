using System.Xml.Serialization;
using Assets.Scripts.Objects.Items;

namespace Assets.Scripts.Objects.Motherboards;

[XmlInclude(typeof(CommsMotherboardSaveData))]
public class CommsMotherboardSaveData : MotherboardSaveData
{
	public int EnumFilterInt;

	public int SelectedDishIndex;

	public int SelectedPadIndex;
}
