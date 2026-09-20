using TMPro;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class WindowTitleBar : UserInterfaceBase
{
	public TMP_Text TitleText;

	public Image Background;

	public void SetTitle(string titleText)
	{
		TitleText.text = titleText;
	}
}
