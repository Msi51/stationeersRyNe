using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColourWheel : MonoBehaviour
{
	public delegate void OnColourChange();

	public TextMeshProUGUI TextTitle;

	public ColourWheelPicker ColourWheelPicker;

	public Image ColourDisplay;

	public Color Colour = Color.white;

	public TMP_InputField RedField;

	public TMP_InputField GreenField;

	public TMP_InputField BlueField;

	public TMP_InputField HueField;

	public TMP_InputField SaturationField;

	public TMP_InputField BrightnessField;

	public OnColourChange OnColourChangeEvent;

	private bool _HasUpdate;

	public void ToggleColourWheel(bool toggle)
	{
		base.gameObject.SetActive(toggle);
	}

	public void UpdateSelectedColour()
	{
		if (!_HasUpdate)
		{
			Color color = new Color(Colour.r, Colour.g, Colour.b);
			if (int.TryParse(RedField.text, out var result))
			{
				color.r = ConvertNormalToUnity(result);
			}
			if (int.TryParse(GreenField.text, out var result2))
			{
				color.g = ConvertNormalToUnity(result2);
			}
			if (int.TryParse(BlueField.text, out var result3))
			{
				color.b = ConvertNormalToUnity(result3);
			}
			Color.RGBToHSV(color, out var H, out var S, out var V);
			if (float.TryParse(HueField.text, out var result4))
			{
				color = Color.HSVToRGB(ConvertNormalToUnity(result4), S, V);
			}
			Color.RGBToHSV(color, out H, out S, out V);
			if (float.TryParse(SaturationField.text, out var result5))
			{
				color = Color.HSVToRGB(H, ConvertNormalToUnity(result5), V);
			}
			Color.RGBToHSV(color, out H, out S, out V);
			if (float.TryParse(BrightnessField.text, out var result6))
			{
				color = Color.HSVToRGB(H, S, ConvertNormalToUnity(result6));
			}
			SetCustomTerrainColour(color);
		}
	}

	public void UpdateRGBValues(Color colour)
	{
		_HasUpdate = true;
		RedField.text = ConvertUnityToNormal(colour.r).ToString(CultureInfo.InvariantCulture);
		GreenField.text = ConvertUnityToNormal(colour.g).ToString(CultureInfo.InvariantCulture);
		BlueField.text = ConvertUnityToNormal(colour.b).ToString(CultureInfo.InvariantCulture);
		Color.RGBToHSV(colour, out var H, out var S, out var V);
		HueField.text = ConvertUnityToNormal(H).ToString(CultureInfo.InvariantCulture);
		SaturationField.text = ConvertUnityToNormal(S).ToString(CultureInfo.InvariantCulture);
		BrightnessField.text = ConvertUnityToNormal(V).ToString(CultureInfo.InvariantCulture);
		_HasUpdate = false;
	}

	public int ConvertUnityToNormal(float value)
	{
		return (int)(value * 255f);
	}

	public int ConvertUnityToNormal(int value)
	{
		return value * 255;
	}

	public float ConvertNormalToUnity(int value)
	{
		return (float)value / 255f;
	}

	public float ConvertNormalToUnity(float value)
	{
		return value / 255f;
	}

	public void SetCustomTerrainColour(Color colour, bool updateValues = false)
	{
		Colour = colour;
		if ((bool)ColourDisplay)
		{
			ColourDisplay.color = colour;
		}
		if (updateValues)
		{
			UpdateRGBValues(colour);
		}
		if (OnColourChangeEvent != null)
		{
			OnColourChangeEvent();
		}
	}
}
