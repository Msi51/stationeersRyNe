using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Genetics;

public class MultiGeneInfo : MonoBehaviour
{
	[SerializeField]
	private ValueRangeDisplay _valueRangeDisplay;

	[SerializeField]
	private TextMeshProUGUI _textMesh;

	public void SetValues(string text, float minPossible, float maxPossible, float outerMin, float outerMax, float innerMin, float innerMax, AnimationCurve scalingCurve, ValueDisplay.Unit unit)
	{
		_textMesh.text = text;
		_valueRangeDisplay.SetValue(minPossible, maxPossible, outerMin, outerMax, innerMin, innerMax, scalingCurve, unit);
	}
}
