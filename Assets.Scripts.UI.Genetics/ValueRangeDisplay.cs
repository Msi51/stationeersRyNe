using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Genetics;

public class ValueRangeDisplay : MonoBehaviour
{
	[SerializeField]
	private RectTransform _outerBar;

	[SerializeField]
	private RectTransform _innerBar;

	[SerializeField]
	private RectTransform _rootTransform;

	[SerializeField]
	private TextMeshProUGUI _textMinPossible;

	[SerializeField]
	private TextMeshProUGUI _textMaxPossible;

	[SerializeField]
	private TextMeshProUGUI _textOuterMin;

	[SerializeField]
	private TextMeshProUGUI _textOuterMax;

	[SerializeField]
	private TextMeshProUGUI _textInnerMin;

	[SerializeField]
	private TextMeshProUGUI _textInnerMax;

	public void SetValue(float minPossible, float maxPossible, float outerMin, float outerMax, float innerMin, float innerMax, AnimationCurve scalingCurve, ValueDisplay.Unit unit)
	{
		float num = _rootTransform.sizeDelta.x - 2f;
		_textMinPossible.text = ValueDisplay.GetUnitValue(minPossible, unit);
		_textMaxPossible.text = ValueDisplay.GetUnitValue(maxPossible, unit);
		_textOuterMin.text = ValueDisplay.GetUnitValue(outerMin, unit);
		_textOuterMax.text = ValueDisplay.GetUnitValue(outerMax, unit);
		_textInnerMin.text = ValueDisplay.GetUnitValue(innerMin, unit);
		_textInnerMax.text = ValueDisplay.GetUnitValue(innerMax, unit);
		float num2 = scalingCurve.Evaluate(outerMin);
		float num3 = scalingCurve.Evaluate(outerMax);
		float num4 = scalingCurve.Evaluate(innerMin);
		float num5 = scalingCurve.Evaluate(innerMax);
		float x = num * num2;
		float num6 = num * num3;
		float x2 = num * num4;
		float num7 = num * num5;
		_outerBar.offsetMin = new Vector2(x, 0f);
		_outerBar.offsetMax = new Vector2(num6 - num, 0f);
		_innerBar.offsetMin = new Vector2(x2, 0f);
		_innerBar.offsetMax = new Vector2(num7 - num, 0f);
	}
}
