using System.Collections.Generic;

namespace Assets.Scripts.UI;

public class DifficultySettingWindow : InputListWindow
{
	public List<DifficultyButtonItem> Buttons = new List<DifficultyButtonItem>();

	public void Register(DifficultyButtonItem inProgressDifficulty)
	{
		Buttons.Add(inProgressDifficulty);
	}
}
