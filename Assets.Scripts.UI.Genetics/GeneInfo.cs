using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Genetics;

public class GeneInfo : MonoBehaviour
{
	[SerializeField]
	private ValueDisplay _valueDisplay;

	[SerializeField]
	private TextMeshProUGUI _textMesh;

	public void SetValues(string text, float baseValue, float value, float min, float max, ValueDisplay.Unit unit)
	{
		_textMesh.text = text;
		_valueDisplay.SetValue(baseValue, value, min, max, unit);
	}
}
