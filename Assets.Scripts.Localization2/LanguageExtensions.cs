using System;
using System.Collections.Generic;

namespace Assets.Scripts.Localization2;

public static class LanguageExtensions
{
	public static Localization.Language PopulateFrom(this Localization.Language self, Dictionary<string, string> dict)
	{
		Dictionary<string, Localization.RecordThing> dictionary = new Dictionary<string, Localization.RecordThing>();
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		Dictionary<string, Localization.RecordReagent> dictionary3 = new Dictionary<string, Localization.RecordReagent>();
		Dictionary<string, string> dictionary4 = new Dictionary<string, string>();
		string key3;
		foreach (KeyValuePair<string, string> item3 in dict)
		{
			string key = item3.Key;
			string value = item3.Value;
			switch (key)
			{
			case "Language":
				self.Name = value;
				continue;
			case "Font":
				self.Font = value;
				continue;
			case "Code":
				Enum.TryParse<LanguageCode>(value, out self.Code);
				continue;
			}
			int num = key.IndexOf('_');
			string obj = ((num > 0) ? key.Substring(0, num) : null);
			string key2 = ((num > 0) ? key.Substring(num + 1) : key);
			key3 = obj?.ToLowerInvariant();
			switch (key3)
			{
			case "reagent":
				dictionary3[key2] = new Localization.RecordReagent
				{
					Key = key2,
					Value = value
				};
				break;
			case "reagentunit":
				dictionary4[key2] = value;
				break;
			case "gas":
				self.Gases.Add(new Localization.Record
				{
					Key = key2,
					Value = value
				});
				break;
			case "action":
				self.Actions.Add(new Localization.Record
				{
					Key = key2,
					Value = value
				});
				break;
			case "thing":
				if (key.EndsWith("_Description"))
				{
					dictionary2[key2] = value;
				}
				else
				{
					dictionary[key2] = new Localization.RecordThing(key2, value);
				}
				break;
			case "slot":
				self.Slots.Add(new Localization.Record
				{
					Key = key2,
					Value = value
				});
				break;
			case "interactable":
				self.Interactables.Add(new Localization.Record
				{
					Key = key2,
					Value = value
				});
				break;
			case "interface":
				self.Interface.Add(new Localization.Record
				{
					Key = key2,
					Value = value
				});
				break;
			case "color":
				self.Colors.Add(new Localization.Record
				{
					Key = key2,
					Value = value
				});
				break;
			case "key":
				self.Keys.Add(new Localization.Record
				{
					Key = key2,
					Value = value
				});
				break;
			case "mineable":
				self.Mineables.Add(new Localization.Record
				{
					Key = key2,
					Value = value
				});
				break;
			case "screenspacetooltip":
				self.ScreenSpaceToolTips.Add(new Localization.Record
				{
					Key = key2,
					Value = value
				});
				break;
			case "gamestring":
				self.GameStrings.Add(new Localization.Record
				{
					Key = key2,
					Value = value
				});
				break;
			}
		}
		foreach (KeyValuePair<string, Localization.RecordReagent> item4 in dictionary3)
		{
			if (dictionary2.TryGetValue(item4.Key, out var value2))
			{
				item4.Value.Unit = value2;
			}
		}
		self.Reagents.Clear();
		foreach (KeyValuePair<string, Localization.RecordReagent> item5 in dictionary3)
		{
			item5.Deconstruct(out key3, out var value3);
			Localization.RecordReagent item = value3;
			self.Reagents.Add(item);
		}
		foreach (KeyValuePair<string, Localization.RecordThing> item6 in dictionary)
		{
			if (dictionary2.TryGetValue(item6.Key, out var value4))
			{
				item6.Value.ThingDescription = value4;
			}
		}
		self.Things.Clear();
		foreach (KeyValuePair<string, Localization.RecordThing> item7 in dictionary)
		{
			item7.Deconstruct(out key3, out var value5);
			Localization.RecordThing item2 = value5;
			self.Things.Add(item2);
		}
		return self;
	}
}
