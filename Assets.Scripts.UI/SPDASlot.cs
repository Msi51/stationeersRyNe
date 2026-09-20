using Assets.Scripts.Localization2;
using TMPro;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class SPDASlot : UserInterfaceBase
{
	public Image SlotImage;

	public TextMeshProUGUI SlotTitle;

	public TextMeshProUGUI SlotTypeText;

	public TextMeshProUGUI SlotIndex;

	public void PopulateSlotType(string type)
	{
		SlotTypeText.text = $"<color=#FFFFFF80>{GameStrings.SPDASlotType}</color>{type}";
	}

	public void PopulateSlotIndex(string index)
	{
		SlotIndex.text = $"<color=#FFFFFF80>{GameStrings.SPDASlotIndex}</color>{index}";
	}
}
