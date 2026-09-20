using UnityEngine;
using UnityEngine.UI;

public class SetSliderValue : MonoBehaviour
{
	private Slider slider;

	public InputField TargetField;

	private void Awake()
	{
		slider = GetComponent<Slider>();
		OnChangedValue();
	}

	public void OnChangedValue()
	{
		TargetField.text = slider.value.ToString();
	}
}
