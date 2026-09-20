using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Xml.Linq;
using Assets.Scripts.Util;

namespace Assets.Scripts.Localization2;

public class ResxGenerator
{
	private HashSet<string> _addedKeys = new HashSet<string>();

	private List<string> _duplicates = new List<string>();

	private int _invalidKeyCount;

	private int _invalidValueCount;

	private int _totalRecordsAttempted;

	private int _totalRecordsAdded;

	private Localization.LanguageFolder _languageFolder;

	private string _locale;

	private const string NAME_DEFINE = "name";

	public ResxGenerator(Localization.LanguageFolder languageFolder, string locale)
	{
		_languageFolder = languageFolder;
		_locale = locale;
	}

	public void Generate()
	{
		CultureInfo cultureInfo = CultureInfo.GetCultureInfo(_locale);
		ConsoleWindow.PrintAction("found language '" + cultureInfo.Name + "'");
		string text = cultureInfo.Name + ".resx";
		string text2 = Path.Join(Defines.Paths.LocalData, text);
		if (File.Exists(text2))
		{
			File.Delete(text2);
		}
		ConsoleWindow.Print("generating " + text2);
		using (ResXResourceWriter resXResourceWriter = new ResXResourceWriter(text2))
		{
			resXResourceWriter.AddResource("Font", "font_english");
			resXResourceWriter.AddResource("Code", Localization.CurrentLanguage.ToString());
			resXResourceWriter.AddResource("Language", cultureInfo.DisplayName);
			AddResourcesFromLanguageFolder(resXResourceWriter);
		}
		string text3 = "special value for language font";
		string text4 = "special value for language display name";
		XDocument xDocument = XDocument.Load(text2);
		XElement xElement = xDocument.Element("root");
		if (xElement == null)
		{
			return;
		}
		xElement.AddBeforeSelf(new XComment("Parsed values are included in capitals surrounded by braces, such as {NAME}.\nThe contents of the braces themselves needs to be preserved, but they may freely be moved around within the string.\nThe comment field includes the default english string at the time of file generation so is useful for determining when a string has changed.\nStrings for the in-game console typically are printed with no capitalization so will be listed here uncapitalized.\nThe filename is used for the locale Code according to ISO 639-1. By default the language name is taken from this. To override this, enter an entry for 'Language'.\nThe 'Font' value is a special entry that will be used to select which game font is to be used to allow for special characters."));
		foreach (XElement item in xElement.Elements("data").ToList())
		{
			string value = item.Value;
			if (!(value == "name"))
			{
				if (value == "Font")
				{
					item.AddBeforeSelf(new XComment("* " + text3 + " *"));
					item.Add(new XElement("comment", text3));
				}
				else
				{
					item.Add(new XElement("comment", item.Value));
				}
			}
			else
			{
				item.AddBeforeSelf(new XComment("* " + text4 + " *"));
				item.Add(new XElement("comment", text4));
			}
		}
		xDocument.Save(text2);
	}

	private void ClearResults()
	{
		_duplicates.Clear();
		_invalidKeyCount = 0;
		_invalidValueCount = 0;
		_totalRecordsAttempted = 0;
		_totalRecordsAdded = 0;
	}

	private void AddResource(ResXResourceWriter writer, string key, string value, string prefix = "", string postfix = "", bool ignoreDuplicateKeys = false)
	{
		_totalRecordsAttempted++;
		if (string.IsNullOrEmpty(key))
		{
			_invalidKeyCount++;
			return;
		}
		if (string.IsNullOrEmpty(value))
		{
			_invalidValueCount++;
			return;
		}
		string obj = (string.IsNullOrEmpty(prefix) ? string.Empty : (prefix + "_"));
		string text = (string.IsNullOrEmpty(postfix) ? string.Empty : ("_" + postfix));
		string text2 = obj + key + text;
		if (_addedKeys.Contains(text2))
		{
			if (!ignoreDuplicateKeys)
			{
				_duplicates.Add(text2);
			}
		}
		else
		{
			writer.AddResource(text2, value);
			_addedKeys.Add(text2);
			_totalRecordsAdded++;
		}
	}

	private void AddResourcesFromLanguageFolder(ResXResourceWriter writer)
	{
		_addedKeys.Clear();
		int num = 0;
		foreach (Localization.Language languagePage in _languageFolder.LanguagePages)
		{
			ClearResults();
			AddResourcesFromLanguage(writer, languagePage, num);
			ReportResults(num);
			num++;
		}
	}

	private void AddResourcesFromLanguage(ResXResourceWriter writer, Localization.Language _language, int pageIndex)
	{
		foreach (Localization.RecordReagent reagent in _language.Reagents)
		{
			AddResource(writer, reagent.Key, reagent.Value, "Reagent");
		}
		foreach (Localization.RecordReagent reagent2 in _language.Reagents)
		{
			AddResource(writer, reagent2.Key, reagent2.Unit, "Reagent", "Unit", ignoreDuplicateKeys: true);
		}
		foreach (Localization.Record gase in _language.Gases)
		{
			AddResource(writer, gase.Key, gase.Value, "Gas");
		}
		foreach (Localization.Record action in _language.Actions)
		{
			AddResource(writer, action.Key, action.Value, "Action");
		}
		foreach (Localization.RecordThing thing in _language.Things)
		{
			AddResource(writer, thing.Key, thing.Value, "Thing");
			AddResource(writer, thing.Key, thing.ThingDescription, "Thing", "Description");
		}
		foreach (Localization.Record slot in _language.Slots)
		{
			AddResource(writer, slot.Key, slot.Value, "Slot");
		}
		foreach (Localization.Record interactable in _language.Interactables)
		{
			AddResource(writer, interactable.Key, interactable.Value, "Interactable");
		}
		foreach (Localization.Record item in _language.Interface)
		{
			AddResource(writer, item.Key, item.Value, "Interface");
		}
		foreach (Localization.Record color in _language.Colors)
		{
			AddResource(writer, color.Key, color.Value, "Color");
		}
		foreach (Localization.Record key in _language.Keys)
		{
			AddResource(writer, key.Key, key.Value, "Key", "page" + pageIndex);
		}
		foreach (Localization.Record mineable in _language.Mineables)
		{
			AddResource(writer, mineable.Key, mineable.Value, "Mineable");
		}
		foreach (Localization.Record screenSpaceToolTip in _language.ScreenSpaceToolTips)
		{
			AddResource(writer, screenSpaceToolTip.Key, screenSpaceToolTip.Value, "ScreenSpaceToolTip");
		}
		foreach (Localization.Record gameString in _language.GameStrings)
		{
			AddResource(writer, gameString.Key, gameString.Value, "GameString");
		}
	}

	private void ReportResults(int pageIndex)
	{
		bool flag = false;
		ConsoleWindow.PrintAction(_locale + " page " + pageIndex + ":");
		ConsoleWindow.Print("Total records attempted " + _totalRecordsAttempted + ".");
		ConsoleWindow.Print("Total records added " + _totalRecordsAdded + ".");
		if (_invalidKeyCount > 0)
		{
			flag = true;
			ConsoleWindow.Print(_invalidKeyCount + " keys were either null or empty and have been skipped.");
		}
		if (_invalidValueCount > 0)
		{
			flag = true;
			ConsoleWindow.Print(_invalidValueCount + " values were either null or empty and have been skipped.");
		}
		if (_duplicates.Count > 0)
		{
			flag = true;
			foreach (string duplicate in _duplicates)
			{
				ConsoleWindow.Print("Tried to add " + duplicate + " but was found to be a duplicate.");
			}
		}
		if (!flag)
		{
			ConsoleWindow.Print("No errors.");
		}
	}
}
