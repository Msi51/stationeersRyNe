using UnityEngine;
using UnityEngine.UI;

namespace LeTai.Asset.TranslucentImage.Demo;

public class PlatformPreset : MonoBehaviour
{
	public Preset[] presets;

	private void Start()
	{
		Slider component = GameObject.Find("Size Slider").GetComponent<Slider>();
		Slider component2 = GameObject.Find("Iteration Slider").GetComponent<Slider>();
		Slider component3 = GameObject.Find("Downsample Slider").GetComponent<Slider>();
		Slider component4 = GameObject.Find("Max update rate Slider").GetComponent<Slider>();
		Preset[] array = presets;
		for (int i = 0; i < array.Length; i++)
		{
			Preset preset = array[i];
			if (preset.platform == Application.platform)
			{
				component.value = preset.size;
				component2.value = preset.iteration;
				component3.value = preset.downsample;
				component4.value = preset.maxUpdateRate;
			}
		}
	}
}
