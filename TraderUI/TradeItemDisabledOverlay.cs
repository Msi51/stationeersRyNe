using TMPro;
using UnityEngine;

namespace TraderUI;

public class TradeItemDisabledOverlay : GameBase
{
	[SerializeField]
	private TextMeshProUGUI _textMesh;

	public void Show(string text)
	{
		_textMesh.text = text;
		GameObject.SetActive(value: true);
	}

	public void Hide()
	{
		GameObject.SetActive(value: false);
	}
}
