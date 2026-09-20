using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class ScanIcon : GameBase
{
	private Color _color;

	[SerializeField]
	private Color _disabledColor;

	[SerializeField]
	private TextMeshProUGUI _textTMP;

	[SerializeField]
	private Image _icon;

	private void Awake()
	{
		_color = _icon.color;
	}

	public void SetText(string text)
	{
		_textTMP.text = text;
	}

	public void SetEnabled(bool enabled)
	{
		_icon.color = (enabled ? _color : _disabledColor);
		_textTMP.gameObject.SetActive(enabled);
	}
}
