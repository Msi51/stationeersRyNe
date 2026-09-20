using System.Text;
using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;

public class NewWorldDifficulty : UserInterfaceBase
{
	public WorldDescription Parent;

	public TMP_Text DifficultyTitle;

	public TMP_Text DifficultyText;

	public void SetDifficulty(DifficultySetting setting)
	{
		DifficultyTitle.text = setting.Name;
		StringBuilder stringBuilder = new StringBuilder();
		DisplayOnlyIf(setting.Creative, stringBuilder, GameStrings.DifficultyValueStatus, GameStrings.DifficultyStatisticCreativeMode);
		DisplayOnlyIfNot(setting.Achievements, stringBuilder, GameStrings.DifficultyValueStatusAre, GameStrings.DifficultyStatisticAchievements);
		bool value = setting.EatWhileHelmetClosed.Value;
		bool value2 = setting.DrinkWhileHelmetClosed.Value;
		if (!value)
		{
			if (value2)
			{
				stringBuilder.AppendLine(GameStrings.DifficultyCannotEatThroughHelmet.DisplayString);
			}
			else
			{
				stringBuilder.AppendLine(GameStrings.DifficultyCannotEatOrDrinkThroughHelmet.DisplayString);
			}
		}
		else if (!value2)
		{
			stringBuilder.AppendLine(GameStrings.DifficultyCannotDrinkThroughHelmet.DisplayString);
		}
		WorldPresetItem selectedWorld = NewWorldMenu.SelectedWorld;
		if ((object)selectedWorld != null && selectedWorld.HasStorms())
		{
			DisplaySmartStat(setting.WeatherLanderDamageRate, stringBuilder, GameStrings.DifficultyStatisticWeatherDamage, Localization.GetThingName("Lander"), DifficultySetting.Default.WeatherLanderDamageRate, alwaysShow: true);
			DisplaySmartStat(setting.StartingWeatherMultiplier, stringBuilder, GameStrings.DifficultyStatisticCalmWeather, DifficultySetting.Default.StartingWeatherMultiplier, alwaysShow: false, "red", "green", GameStrings.DifficultyValuePeriod);
		}
		if (setting.HungerRate.IsZero() && setting.HydrationRate.IsZero() && setting.BreathingRate.IsZero() && setting.RobotBatteryRate.IsZero())
		{
			DisplayAlways(value: false, stringBuilder, GameStrings.DifficultyValueStatus, GameStrings.DifficultyStatisticMetabolism, "red", "green");
		}
		else if (setting.HungerRate.Approximately(DifficultySetting.Default.HungerRate) && setting.HydrationRate.Approximately(DifficultySetting.Default.HydrationRate) && setting.BreathingRate.Approximately(DifficultySetting.Default.BreathingRate) && setting.RobotBatteryRate.Approximately(DifficultySetting.Default.RobotBatteryRate))
		{
			DisplayAlways(value: true, stringBuilder, GameStrings.DifficultyValueStatus, GameStrings.DifficultyStatisticMetabolism, "red", "green");
			DisplayAlways(setting.Sanitation, stringBuilder, GameStrings.DifficultyValueStatus, GameStrings.DifficultyStatisticSanitation, "red", "green");
		}
		else if (DifficultySetting.MetabolismApproximately(setting))
		{
			DisplaySmartStat(setting.HungerRate, stringBuilder, GameStrings.DifficultyStatisticMetabolism, DifficultySetting.Default.HungerRate);
			DisplayAlways(setting.Sanitation, stringBuilder, GameStrings.DifficultyValueStatus, GameStrings.DifficultyStatisticSanitation, "red", "green");
		}
		else
		{
			DisplaySmartStat(setting.HungerRate, stringBuilder, GameStrings.DifficultyStatisticHungerRate, DifficultySetting.Default.HungerRate);
			DisplaySmartStat(setting.HydrationRate, stringBuilder, GameStrings.DifficultyStatisticHydrationRate, DifficultySetting.Default.HydrationRate);
			DisplaySmartStat(setting.BreathingRate, stringBuilder, GameStrings.DifficultyStatisticBreathing, DifficultySetting.Default.BreathingRate);
			DisplaySmartStat(setting.RobotBatteryRate, stringBuilder, GameStrings.DifficultyStatisticRobotBattery, DifficultySetting.Default.RobotBatteryRate);
			DisplayAlways(setting.Sanitation, stringBuilder, GameStrings.DifficultyValueStatus, GameStrings.DifficultyStatisticSanitation, "red", "green");
		}
		DisplaySmartStat(setting.MoodRate, stringBuilder, GameStrings.DifficultyStatisticMoodReduction, DifficultySetting.Default.MoodRate);
		DisplaySmartStat(setting.HygieneRate, stringBuilder, GameStrings.DifficultyStatisticHygieneReduction, DifficultySetting.Default.HygieneRate);
		DisplaySmartStat(setting.FoodDecayRate, stringBuilder, GameStrings.DifficultyStatisticFoodDecay, DifficultySetting.Default.FoodDecayRate);
		DisplaySmartStat(setting.JetpackRate, stringBuilder, GameStrings.DifficultyStatisticJetpackConsumption, DifficultySetting.Default.JetpackRate);
		DisplaySmartStat(setting.MiningYield, stringBuilder, GameStrings.DifficultyStatisticMining, DifficultySetting.Default.MiningYield, alwaysShow: false, "red", "green", GameStrings.DifficultyValueYield);
		DisplaySmartStat(setting.LungDamageRate, stringBuilder, GameStrings.DifficultyStatisticLungDamage, DifficultySetting.Default.LungDamageRate, alwaysShow: true);
		DisplayPercentStat(setting.OfflineMetabolism, stringBuilder, GameStrings.DifficultyStatisticOfflineMetabolism);
		DifficultyText.text = stringBuilder.ToString();
	}

	private void DisplaySmartStat(float value, StringBuilder text, Assets.Scripts.Localization2.GameString statistic, float normal = 1f, bool alwaysShow = false, string colorUnder1 = "green", string colorOver1 = "red", Assets.Scripts.Localization2.GameString rateString = null)
	{
		if (rateString == null)
		{
			rateString = GameStrings.DifficultyValueRate;
		}
		if ((double)Mathf.Abs(value) <= 0.0001)
		{
			text.AppendLine(GameStrings.DifficultyValueStatus.AsString(statistic.AsColor("yellow"), GameStrings.DisabledLower.AsColor(colorUnder1)));
			return;
		}
		int num = Mathf.RoundToInt(value / normal * 100f);
		if (num == 100)
		{
			if (alwaysShow)
			{
				text.AppendLine(GameStrings.DifficultyValueStatus.AsString(statistic.AsColor("yellow"), GameStrings.EnabledLower.AsColor(colorOver1)));
			}
		}
		else
		{
			text.AppendLine(rateString.AsString(statistic.AsColor("yellow"), GetSmartStatistic(num, colorUnder1, colorOver1)));
		}
	}

	private void DisplaySmartStat(float value, StringBuilder text, Assets.Scripts.Localization2.GameString statistic, string thing, float normal = 1f, bool alwaysShow = false, string colorUnder1 = "green", string colorOver1 = "red", Assets.Scripts.Localization2.GameString rateString = null)
	{
		if (rateString == null)
		{
			rateString = GameStrings.DifficultyThingValueRate;
		}
		thing = thing.ToString("white");
		if ((double)Mathf.Abs(value) <= 0.0001)
		{
			text.AppendLine(GameStrings.DifficultyThingStatus.AsString(statistic.AsColor("yellow"), thing, GameStrings.DisabledLower.AsColor(colorUnder1)));
			return;
		}
		int num = Mathf.RoundToInt(value / normal * 100f);
		if (num == 100)
		{
			if (alwaysShow)
			{
				text.AppendLine(GameStrings.DifficultyThingStatus.AsString(statistic.AsColor("yellow"), thing, GameStrings.EnabledLower.AsColor(colorOver1)));
			}
		}
		else
		{
			text.AppendLine(rateString.AsString(statistic.AsColor("yellow"), thing, GetSmartStatistic(num, colorUnder1, colorOver1)));
		}
	}

	private void DisplayPercentStat(float value, StringBuilder text, Assets.Scripts.Localization2.GameString statistic, float normal = 1f, string colorUnder1 = "green", string colorOver1 = "red")
	{
		if ((double)Mathf.Abs(value) <= 0.0001)
		{
			text.AppendLine(GameStrings.DifficultyValueStatus.AsString(statistic.AsColor("yellow"), GameStrings.DisabledLower.AsColor(colorUnder1)));
			return;
		}
		int value2 = Mathf.RoundToInt(value / normal * 100f);
		text.AppendLine(GameStrings.DifficultyValueRate.AsString(statistic.AsColor("yellow"), GetSmartStatistic(value2, colorUnder1, colorOver1)));
	}

	public static string GetSmartStatistic(int value, string colorUnder1 = "green", string colorOver1 = "red")
	{
		string text = ((value >= 100) ? colorOver1 : colorUnder1);
		if (value % 50 == 0 && value != 0 && value != 100)
		{
			if (value % 100 == 0)
			{
				return $"<color={text}>x{(float)value / 100f:F0}</color>";
			}
			return $"<color={text}>x{(float)value / 100f:F1}</color>";
		}
		return "<color=" + text + ">" + value + "%</color>";
	}

	private void DisplayOnlyIf(bool value, StringBuilder text, Assets.Scripts.Localization2.GameString displayString, Assets.Scripts.Localization2.GameString action)
	{
		if (value)
		{
			text.AppendLine(displayString.AsString(action.AsColor("yellow"), GameStrings.EnabledLower.AsColor("green")));
		}
	}

	private void DisplayOnlyIfNot(bool value, StringBuilder text, Assets.Scripts.Localization2.GameString displayString, Assets.Scripts.Localization2.GameString action)
	{
		if (!value)
		{
			text.AppendLine(displayString.AsString(action.AsColor("yellow"), GameStrings.DisabledLower.AsColor("red")));
		}
	}

	private void DisplayAlways(bool value, StringBuilder text, Assets.Scripts.Localization2.GameString displayString, Assets.Scripts.Localization2.GameString action, string colorEnabled = "green", string colorDisabled = "red")
	{
		text.AppendLine(displayString.AsString(action.AsColor("yellow"), value ? GameStrings.EnabledLower.AsColor(colorEnabled) : GameStrings.DisabledLower.AsColor(colorDisabled)));
	}

	public void SetDifficulty()
	{
		SetDifficulty(WorldConfigurationMenu.SelectedDifficulty);
	}
}
