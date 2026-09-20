using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI.LoadGame;

public class BorderButton : UserInterfaceBase
{
	public Button Button;

	[SerializeField]
	private GameObject _border;

	public void ShowBorder(bool show)
	{
		_border.SetActive(show);
	}
}
