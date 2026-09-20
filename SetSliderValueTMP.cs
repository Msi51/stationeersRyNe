using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SetSliderValueTMP : MonoBehaviour
{
	public Slider TargetSlider;

	public TMP_InputField TargetField;

	private void Awake()
	{
		OnChangedValue();
	}

	public void OnChangedValue()
	{
		TargetField.text = TargetSlider.value.ToString();
	}

	public void OnFinishedInputValue()
	{
		float result2;
		if (int.TryParse(TargetField.text, out var result))
		{
			if ((float)result < TargetSlider.minValue)
			{
				result = (int)TargetSlider.minValue;
			}
			if ((float)result > TargetSlider.maxValue)
			{
				result = (int)TargetSlider.maxValue;
			}
			TargetField.text = result.ToString();
			TargetSlider.value = result;
		}
		else if (float.TryParse(TargetField.text, out result2))
		{
			if (result2 < TargetSlider.minValue)
			{
				result2 = TargetSlider.minValue;
			}
			if (result2 > TargetSlider.maxValue)
			{
				result2 = TargetSlider.maxValue;
			}
			TargetField.text = result2.ToString();
			TargetSlider.value = result2;
		}
	}
}
