using Assets.Scripts.Util;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI;

public class WorldDescription : UserInterfaceBase
{
	public TMP_Text Title;

	public TMP_Text Description;

	public TMP_Text Rating;

	public Gradient TemperatureGradient = new Gradient();

	public Gradient PressureGradient = new Gradient();

	public NewWorldSummary SummaryInfo;

	public NewWorldDifficulty DifficultyInfo;

	public string GetTempText(float kelvin, string value = "")
	{
		float num = (float)((double)kelvin - 273.15);
		float value2 = 0.5f + num / 400f;
		value2 = Mathf.Clamp01(value2);
		string text = "#" + ColorUtility.ToHtmlStringRGB(TemperatureGradient.Evaluate(value2));
		if (!string.IsNullOrEmpty(value))
		{
			return value.ToString(text);
		}
		return num.ToStringRounded(text);
	}

	public string GetPressureText(float kiloPascals, string value = "")
	{
		float value2 = 0.5f + (kiloPascals - 101.325f) / 405.3f;
		value2 = Mathf.Clamp01(value2);
		string text = "#" + ColorUtility.ToHtmlStringRGB(PressureGradient.Evaluate(value2));
		if (!string.IsNullOrEmpty(value))
		{
			return value.ToString(text);
		}
		return kiloPascals.ToStringRounded(text);
	}

	public void SetWorld(WorldSetting worldSetting)
	{
		if (worldSetting != null)
		{
			Title.text = worldSetting.Name?.ToString() ?? worldSetting.Id;
			Description.text = worldSetting.Description?.ToString();
			Rating.text = worldSetting.Rating?.ToString();
			SummaryInfo.SetWorld(worldSetting);
			DifficultyInfo.SetDifficulty();
		}
	}

	public void SetTutorial(WorldSetting worldSetting)
	{
		if (worldSetting == null)
		{
			SetBlank();
			return;
		}
		Title.text = worldSetting.Name?.ToString() ?? worldSetting.Id;
		SummaryInfo.SetText(worldSetting.SummaryText?.ToString() ?? string.Empty);
		Rating.text = worldSetting.Rating?.ToString() ?? string.Empty;
		Description.text = worldSetting.Description?.ToString().AsColor("lightblue") ?? string.Empty;
	}

	public void SetBlank()
	{
		Title.text = string.Empty;
		SummaryInfo.SetText(string.Empty);
		Rating.text = string.Empty;
		Description.text = string.Empty;
	}
}
