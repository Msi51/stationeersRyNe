using TMPro;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class DynamicItem : UserInterfaceBase
{
	public TextMeshProUGUI Name;

	public Toggle Toggle;

	public Image Background;

	public override void SetVisible(bool isVisble)
	{
		UiComponentRenderer.SetVisible(isVisble);
		Toggle.interactable = isVisble;
	}

	public override void SetActive(bool active)
	{
		UiComponentRenderer.SetVisible(active);
		Toggle.interactable = active;
	}
}
