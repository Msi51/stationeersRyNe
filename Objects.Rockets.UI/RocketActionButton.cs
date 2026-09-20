using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class RocketActionButton : MonoBehaviour
{
	public BasicButton Button;

	[SerializeField]
	private Image _selectionOutline;

	public void ShowSelectionOutline(bool show)
	{
		Color color = _selectionOutline.color;
		color.a = ((Button.Interactable && show) ? 1 : 0);
		_selectionOutline.color = color;
	}
}
