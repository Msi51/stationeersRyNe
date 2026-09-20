using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class TutorialInsert : WorldPresetItem
{
	public Button ActionButton;

	public TutorialType TutorialCategory;

	public void ToggleVisibility(TutorialType type)
	{
		SetVisible(type == TutorialCategory || type == TutorialType.Undefined);
	}
}
