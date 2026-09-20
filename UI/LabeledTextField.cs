using Assets.Scripts.UI;
using TMPro;
using UnityEngine;

namespace UI;

public class LabeledTextField : UserInterfaceBase
{
	[Header("Labeled Text Field")]
	[SerializeField]
	private TextMeshProUGUI _labelText;

	[SerializeField]
	private TextMeshProUGUI _valueText;

	public void SetLabel(string text)
	{
		_labelText.text = text;
	}

	public void SetValue(string text)
	{
		_valueText.text = text;
	}
}
