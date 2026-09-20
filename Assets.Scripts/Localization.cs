using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Assets.Scripts.AssetCreation;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Assets.Scripts.Voxel;
using ImGuiNET;
using Reagents;
using TMPro;
using UI.ImGuiUi;
using UnityEngine;

namespace Assets.Scripts;

public static class Localization
{
	public class LocalizationThingDat
	{
		public string PrefabName;

		public string Description;
	}

	public class RegexResult
	{
		public string[] Full;

		public string[] Names;

		public string GetFull(int index)
		{
			return Full[index];
		}

		public string GetName(int index)
		{
			return Names[index];
		}

		public int Count()
		{
			return Full.Length;
		}
	}

	[XmlRoot]
	public class Record : IEquatable<Record>
	{
		[XmlElement]
		public string Key;

		[XmlElement]
		public string Value;

		public Record()
		{
		}

		public Record(LocalizedText localizedText)
		{
			Key = localizedText.StringKey;
			Value = localizedText.TextMesh.text;
		}

		public Record(string key)
		{
			Key = key;
			Value = key.ToProper();
		}

		public override int GetHashCode()
		{
			return Animator.StringToHash(Key);
		}

		public bool Equals(Record other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			return Key == other.Key;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (obj.GetType() != GetType())
			{
				return false;
			}
			return Equals((Record)obj);
		}
	}

	[XmlRoot]
	public class RecordReagent
	{
		[XmlElement]
		public string Key;

		[XmlElement]
		public string Value;

		[XmlElement]
		public string Unit;

		public override int GetHashCode()
		{
			return Animator.StringToHash(Key);
		}
	}

	[XmlRoot]
	public class RecordThing : Record
	{
		[XmlElement("Description")]
		public string ThingDescription;

		public RecordThing()
		{
		}

		public RecordThing(LocalizedText localizedText)
		{
			Key = localizedText.StringKey;
			Value = localizedText.TextMesh.text;
			if (!GameManager.IsBatchMode)
			{
				ThingDescription = Stationpedia.DefaultThingText;
			}
		}

		public RecordThing(string key)
		{
			Key = key;
			Value = ThingAssetCreation.SanitizePrefabName(key);
			if (!GameManager.IsBatchMode)
			{
				ThingDescription = Stationpedia.DefaultThingText;
			}
		}

		public RecordThing(string key, string value)
		{
			Key = key;
			Value = value;
			if (!GameManager.IsBatchMode)
			{
				ThingDescription = Stationpedia.DefaultThingText;
			}
		}
	}

	[XmlRoot]
	public class Language
	{
		[XmlElement]
		public string Name;

		[XmlElement]
		public LanguageCode Code;

		[XmlElement]
		public string Font;

		[XmlArray]
		public List<RecordReagent> Reagents = new List<RecordReagent>(256);

		public List<Record> Gases = new List<Record>(256);

		public List<Record> Actions = new List<Record>(1024);

		public List<RecordThing> Things = new List<RecordThing>(4094);

		public List<Record> Slots = new List<Record>(128);

		public List<Record> Interactables = new List<Record>(64);

		public List<Record> Interface = new List<Record>(4094);

		public List<Record> Colors = new List<Record>(64);

		public List<Record> Keys = new List<Record>(128);

		public List<Record> Mineables = new List<Record>(64);

		public List<Record> ScreenSpaceToolTips = new List<Record>(1024);

		public List<StationpediaPage> HelpPage = new List<StationpediaPage>(1024);

		public List<SPDAThingOverideData> ThingPageOverride = new List<SPDAThingOverideData>(1024);

		public List<SPDAHomePageButtonOverride> HomePageButtonsOverride = new List<SPDAHomePageButtonOverride>(1024);

		public List<Record> GameStrings = new List<Record>();

		[XmlArrayItem("String")]
		public List<string> GameTip = new List<string>();

		public void UpdateRecord(Record stringLocalized, ref List<Record> languageList)
		{
			int num = languageList.FindIndex((Record r) => r.Key == stringLocalized.Key);
			if (num >= 0)
			{
				languageList[num] = stringLocalized;
			}
			else
			{
				languageList.Add(stringLocalized);
			}
		}

		public void UpdateThingRecordLocal(RecordThing stringLocalized, ref List<RecordThing> languageList)
		{
			int num = languageList.FindIndex((RecordThing r) => r.Key == stringLocalized.Key);
			if (num >= 0)
			{
				languageList[num] = stringLocalized;
			}
			else
			{
				languageList.Add(stringLocalized);
			}
		}

		public void UpdateThingRecord(RecordThing localizedThing, Dictionary<int, LocalizationThingDat> dictionary)
		{
			int hashCode = localizedThing.GetHashCode();
			if (dictionary.ContainsKey(hashCode))
			{
				dictionary.Remove(hashCode);
			}
			dictionary.Add(hashCode, new LocalizationThingDat
			{
				Description = localizedThing.ThingDescription,
				PrefabName = localizedThing.Value
			});
		}

		public void UpdateRecord(Record stringLocalized, Dictionary<int, string> dictionary)
		{
			int hashCode = stringLocalized.GetHashCode();
			if (dictionary.ContainsKey(hashCode))
			{
				dictionary.Remove(hashCode);
			}
			dictionary.Add(hashCode, stringLocalized.Value);
		}

		public void Load(bool fallback = false)
		{
			Dictionary<Reagent, string> dictionary = (fallback ? FallbackReagentName : ReagentName);
			Dictionary<Chemistry.GasType, string> dictionary2 = (fallback ? FallbackGasName : GasName);
			Dictionary<int, string> dictionary3 = (fallback ? FallbackKeyName : KeyName);
			Dictionary<int, string> dictionary4 = (fallback ? FallbackActionName : ActionName);
			Dictionary<int, LocalizationThingDat> dictionary5 = (fallback ? FallbackThingsLocalized : ThingLocalized);
			Dictionary<int, string> dictionary6 = (fallback ? FallbackSlotsName : SlotsName);
			Dictionary<int, string> dictionary7 = (fallback ? FallbackInterfaceText : InterfaceText);
			Dictionary<int, string> dictionary8 = (fallback ? FallbackToolTips : ToolTips);
			Dictionary<int, string> dictionary9 = (fallback ? FallbackInteractableName : InteractableName);
			Dictionary<int, string> dictionary10 = (fallback ? FallbackColorNames : ColorNames);
			Dictionary<int, string> dictionary11 = (fallback ? FallbackMineableName : MineableName);
			foreach (RecordReagent reagent2 in Reagents)
			{
				Reagent reagent = Reagent.Generate($"Reagents.{reagent2.Key}");
				if (reagent != null)
				{
					if (dictionary.ContainsKey(reagent))
					{
						dictionary.Remove(reagent);
					}
					dictionary.Add(reagent, reagent2.Value);
				}
			}
			foreach (Record gase in Gases)
			{
				Mole mole = MoleHelper.Generate(gase.Key);
				if (dictionary2.ContainsKey(mole.Type))
				{
					dictionary2.Remove(mole.Type);
				}
				dictionary2.Add(mole.Type, gase.Value);
			}
			foreach (Record key in Keys)
			{
				UpdateRecord(key, dictionary3);
			}
			foreach (Record action in Actions)
			{
				UpdateRecord(action, dictionary4);
			}
			foreach (RecordThing thing in Things)
			{
				UpdateThingRecord(thing, dictionary5);
			}
			foreach (Record slot in Slots)
			{
				UpdateRecord(slot, dictionary6);
			}
			foreach (Record item in Interface)
			{
				UpdateRecord(item, dictionary7);
			}
			foreach (Record screenSpaceToolTip in ScreenSpaceToolTips)
			{
				UpdateRecord(screenSpaceToolTip, dictionary8);
			}
			foreach (Record interactable in Interactables)
			{
				UpdateRecord(interactable, dictionary9);
			}
			foreach (Record color in Colors)
			{
				UpdateRecord(color, dictionary10);
			}
			foreach (Record mineable in Mineables)
			{
				UpdateRecord(mineable, dictionary11);
			}
			if (!fallback && GameTip != null && GameTip.Count > 0)
			{
				GameTips.Clear();
			}
			if (Code == CurrentLanguage)
			{
				foreach (string item2 in GameTip)
				{
					GameTips.Add(item2);
				}
			}
			foreach (SPDAHomePageButtonOverride item3 in HomePageButtonsOverride)
			{
				Stationpedia.HomePageOverrides.Add(item3);
			}
			foreach (SPDAThingOverideData item4 in ThingPageOverride)
			{
				if (item4.ParentListKey == null)
				{
					Stationpedia.DataHandler.HiddenInPedia[item4.ThingName] = item4.HideInSPDA;
					continue;
				}
				if (Stationpedia.DataHandler.ThingOverrideData.ContainsKey(item4.ParentListKey))
				{
					Stationpedia.DataHandler.ThingOverrideData[item4.ParentListKey].Add(item4);
					continue;
				}
				Stationpedia.DataHandler.ThingOverrideData[item4.ParentListKey] = new List<SPDAThingOverideData> { item4 };
			}
			foreach (StationpediaPage item5 in HelpPage)
			{
				Stationpedia.Register(item5, fallback);
			}
		}
	}

	public class LanguageFolder
	{
		public string Name;

		public string FontName;

		public LanguageCode Code;

		public List<Language> LanguagePages = new List<Language>();

		public List<Language> NewPages = new List<Language>();

		public TMP_FontAsset FontAsset;

		private static readonly char[] Delimiters = new char[3] { ' ', '\r', '\n' };

		public LanguageFolder(Language page)
		{
			Name = page.Name;
			Code = page.Code;
			FontName = page.Font;
			LanguagePages.Add(page);
			FontAsset = (string.IsNullOrEmpty(FontName) ? null : Resources.Load<TMP_FontAsset>("UI/" + FontName));
		}

		public int GetCurrentLanguageWordCount()
		{
			int num = 0;
			foreach (string value in ReagentName.Values)
			{
				num += value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
			}
			ConsoleWindow.Print($"reagent names: {num}");
			int num2 = num;
			foreach (string value2 in GasName.Values)
			{
				num += value2.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
			}
			ConsoleWindow.Print($"reagent names: {num - num2}");
			num2 = num;
			foreach (string value3 in ActionName.Values)
			{
				num += value3.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
			}
			ConsoleWindow.Print($"action names: {num - num2}");
			num2 = num;
			foreach (LocalizationThingDat value4 in ThingLocalized.Values)
			{
				if (value4.Description != null)
				{
					num += value4.Description.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
			}
			ConsoleWindow.Print($"thing localized: {num - num2}");
			num2 = num;
			foreach (string value5 in SlotsName.Values)
			{
				num += value5.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
			}
			ConsoleWindow.Print($"Slots Name: {num - num2}");
			num2 = num;
			foreach (string value6 in InterfaceText.Values)
			{
				num += value6.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
			}
			ConsoleWindow.Print($"Interface Text: {num - num2}");
			num2 = num;
			foreach (string value7 in InteractableName.Values)
			{
				num += value7.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
			}
			ConsoleWindow.Print($"Interactable name: {num - num2}");
			num2 = num;
			foreach (List<SPDAThingOverideData> value8 in Stationpedia.DataHandler.ThingOverrideData.Values)
			{
				foreach (SPDAThingOverideData item in value8)
				{
					num += item.CustomCategory.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
					num += item.ThingName.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
			}
			ConsoleWindow.Print($"Override Data: {num - num2}");
			num2 = num;
			foreach (Language languagePage in LanguagePages)
			{
				ConsoleWindow.Print("Language Page: " + languagePage.Name);
				ConsoleWindow.Print($"Language Page Code: {languagePage.Code}");
				foreach (Record action in languagePage.Actions)
				{
					num += action.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"Actions: {num - num2}");
				num2 = num;
				foreach (Record color in languagePage.Colors)
				{
					num += color.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"Colors: {num - num2}");
				num2 = num;
				foreach (Record gase in languagePage.Gases)
				{
					num += gase.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"Gases: {num - num2}");
				num2 = num;
				foreach (Record interactable in languagePage.Interactables)
				{
					num += interactable.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"interactable: {num - num2}");
				num2 = num;
				foreach (Record item2 in languagePage.Interface)
				{
					num += item2.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"interfaceRecord: {num - num2}");
				num2 = num;
				foreach (Record key in languagePage.Keys)
				{
					num += key.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"key: {num - num2}");
				num2 = num;
				foreach (Record mineable in languagePage.Mineables)
				{
					num += mineable.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"mineableineable: {num - num2}");
				num2 = num;
				foreach (RecordReagent reagent in languagePage.Reagents)
				{
					num += reagent.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"reagent: {num - num2}");
				num2 = num;
				foreach (Record slot in languagePage.Slots)
				{
					num += slot.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"slot: {num - num2}");
				num2 = num;
				foreach (RecordThing thing in languagePage.Things)
				{
					if (thing.Value != null)
					{
						num += thing.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
					}
				}
				ConsoleWindow.Print($"thing value: {num - num2}");
				num2 = num;
				foreach (Record gameString in languagePage.GameStrings)
				{
					num += gameString.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"gamestring: {num - num2}");
				num2 = num;
				foreach (string item3 in languagePage.GameTip)
				{
					num += item3.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"gameTip: {num - num2}");
				num2 = num;
				foreach (StationpediaPage item4 in languagePage.HelpPage)
				{
					if (item4.Title != null)
					{
						num += item4.Title.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
					}
				}
				ConsoleWindow.Print($"helpPage title: {num - num2}");
				num2 = num;
				foreach (StationpediaPage item5 in languagePage.HelpPage)
				{
					num += item5.Text.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"helpPage description: {num - num2}");
				num2 = num;
				foreach (Record screenSpaceToolTip in languagePage.ScreenSpaceToolTips)
				{
					num += screenSpaceToolTip.Value.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries).Length;
				}
				ConsoleWindow.Print($"screenSpaceToolTip: {num - num2}");
				num2 = num;
			}
			return num;
		}

		public void Refresh()
		{
			foreach (LocalizedText localizedInterface in LocalizedInterfaces)
			{
				localizedInterface.Refresh();
			}
			foreach (LocalizedFont localizedFontInterface in LocalizedFontInterfaces)
			{
				localizedFontInterface.Refresh();
			}
		}

		public void LoadAll()
		{
			CurrentLanguage = Code;
			ReagentName.Clear();
			GasName.Clear();
			ActionName.Clear();
			ThingLocalized.Clear();
			SlotsName.Clear();
			InterfaceText.Clear();
			InteractableName.Clear();
			Stationpedia.DataHandler.ThingOverrideData.Clear();
			CurrentFont = _currentLanguageFolder.FontAsset;
			foreach (Language languagePage in LanguagePages)
			{
				languagePage.Load();
			}
			Refresh();
			List<Record> list = new List<Record>();
			foreach (Language languagePage2 in LanguagePages)
			{
				foreach (Record gameString in languagePage2.GameStrings)
				{
					list.Add(gameString);
				}
			}
			Assets.Scripts.Localization2.GameString.UpdateLanguage(list);
		}

		public void LoadNew(bool fallback)
		{
			foreach (Language newPage in NewPages)
			{
				newPage.Load(fallback);
			}
			NewPages.Clear();
		}

		public void LoadFallback()
		{
			foreach (Language languagePage in LanguagePages)
			{
				languagePage.Load(fallback: true);
			}
			Refresh();
		}
	}

	public static HashSet<LocalizedText> LocalizedInterfaces = new HashSet<LocalizedText>();

	public static HashSet<LocalizedFont> LocalizedFontInterfaces = new HashSet<LocalizedFont>();

	public static HashSet<LanguageCode> Languages = new HashSet<LanguageCode>();

	public static Dictionary<LanguageCode, LanguageFolder> LanguageData = new Dictionary<LanguageCode, LanguageFolder>();

	public static TMP_FontAsset CurrentFont;

	private static readonly Dictionary<Reagent, string> ReagentName = new Dictionary<Reagent, string>();

	private static readonly Dictionary<Chemistry.GasType, string> GasName = new Dictionary<Chemistry.GasType, string>();

	private static readonly Dictionary<int, string> KeyName = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> ActionName = new Dictionary<int, string>();

	private static readonly Dictionary<int, LocalizationThingDat> ThingLocalized = new Dictionary<int, LocalizationThingDat>();

	private static readonly Dictionary<int, string> SlotsName = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> InterfaceText = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> InteractableName = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> ColorNames = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> MineableName = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> ToolTips = new Dictionary<int, string>();

	private static readonly List<string> GameTips = new List<string>();

	private static readonly Dictionary<Reagent, string> FallbackReagentName = new Dictionary<Reagent, string>();

	private static readonly Dictionary<Chemistry.GasType, string> FallbackGasName = new Dictionary<Chemistry.GasType, string>();

	private static readonly Dictionary<int, string> FallbackKeyName = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> FallbackActionName = new Dictionary<int, string>();

	private static readonly Dictionary<int, LocalizationThingDat> FallbackThingsLocalized = new Dictionary<int, LocalizationThingDat>();

	private static readonly Dictionary<int, string> FallbackSlotsName = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> FallbackInterfaceText = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> FallbackInteractableName = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> FallbackColorNames = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> FallbackMineableName = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> FallbackToolTips = new Dictionary<int, string>();

	private static readonly string ErrorInterface = "<T:{0}:{1}>";

	private static readonly string ErrorName = "<N:{0}:{1}>";

	private static readonly string ErrorAction = "<A:{0}:{1}>";

	private static readonly string ErrorInteraction = "<I:{0}:{1}>";

	private static readonly string ErrorMineable = "<M:{0}:{1}>";

	public static Action OnLanguageChanged;

	public static LanguageCode CurrentLanguage;

	private static LanguageFolder _currentLanguageFolder;

	private static LanguageCode FallbackLanguage = LanguageCode.EN;

	private static LanguageFolder _fallbackLanguageFolder;

	private const string StringVar1 = "VAR1";

	private const string StringVar2 = "VAR2";

	private const string StringKey = "KEY";

	private const string StringLogicType = "LOGICTYPE";

	private const string StringLogicSlotType = "LOGICSLOTTYPE";

	private const string StringThing = "THING";

	private const string StringGas = "GAS";

	private const string StringReagent = "REAGENT";

	private const string StringColor = "COLOR";

	private const string StringHeader = "HEADER";

	private const string StringPosition = "POS";

	private const string StringLink = "LINK";

	private const string StringList = "LIST";

	private const string StringSlot = "SLOT";

	private const string StringFormat0 = "{0}";

	private const string ColorInput = "<color=#FBB03B>{0}</color>";

	private const string ColorThing = "<link=Thing{1}><color=green>{0}</color></link>";

	private const string ColorSlot = "<link=Slot{1}><color=orange>{0}</color></link>";

	private const string ColorReagent = "<link=Reagent{1}><color=#B566FF>{0}</color></link>";

	public const string HEX_GAS = "#44AD83";

	public const string COLOR_GAS_STRING = "<link=Gas{1}><color=#44AD83>{0}</color></link>";

	private static readonly List<string> Colors = new List<string> { "red", "blue", "yellow", "green", "white", "orange" };

	private const string COMMENT = "comment";

	private const string REGISTER = "register";

	private const string NUMBER = "number";

	private const string STRING = "string";

	public const string COLOR_REGISTER = "<color=#0080FFFF>";

	public const string COLOR_NUMBER = "<color=#20B2AA>";

	public const string COLOR_DEVICE = "<color=green>";

	public const string COLOR_STRING = "<color=white>";

	public const string COLOR_MACRO = "<color=#A0A0A0>";

	public const string COLOR_NETWORK = "<color=#00FFEC>";

	public const string COLOR_COMMENTS = "<color=#585858FF>";

	public const string COLOR_HELP = "<color=#808080>";

	public static List<ScriptCommand> OrderedScriptCommands;

	public static string Variable1;

	public static string Variable2;

	private static string _randomTip;

	private static string _quantityModifierKey;

	public static string QuantityModifierKey
	{
		get
		{
			if (_quantityModifierKey == null)
			{
				_quantityModifierKey = KeyMap._QuantityModifier.Key.ToString();
			}
			return _quantityModifierKey;
		}
	}

	private static string LanguagePath(string path)
	{
		return Path.Combine(path.Replace("\\", "/"), "Language");
	}

	private static void AddLanguagePage(Language page)
	{
		LanguageData.TryGetValue(page.Code, out var value);
		if (value == null)
		{
			LanguageData.Add(page.Code, new LanguageFolder(page));
		}
		else
		{
			value.LanguagePages.Add(page);
		}
	}

	private static void AddNewPage(Language page)
	{
		LanguageData.TryGetValue(page.Code, out var value);
		value?.NewPages.Add(page);
	}

	public static void GetLanguages(string path = "")
	{
		bool flag = string.IsNullOrWhiteSpace(path);
		if (flag)
		{
			path = Application.streamingAssetsPath;
			LanguageData.Clear();
		}
		LoadLanguageFilesFromXml(path, flag);
	}

	private static void LoadLanguageFilesFromXml(string path, bool initialLoad)
	{
		string[] files = Directory.GetFiles(LanguagePath(path), "*.xml");
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(Language));
		string[] array = files;
		foreach (string text in array)
		{
			if (!(XmlSerialization.Deserialize(xmlSerializer, text) is Language page))
			{
				Debug.LogError("Error parsing language file: " + text);
			}
			else if (initialLoad)
			{
				AddLanguagePage(page);
			}
			else
			{
				AddNewPage(page);
			}
		}
	}

	private static void LoadLanguageFilesFromResx(string path, bool initialLoad)
	{
		string[] files = Directory.GetFiles(LanguagePath(path), "*.resx");
		List<Language> list = new List<Language>(files.Length);
		string[] array = files;
		foreach (string path2 in array)
		{
			Language item = new Language().PopulateFrom(ResxImporter.LoadDictionaryFromFile(path2));
			list.Add(item);
		}
		foreach (Language item2 in list)
		{
			if (initialLoad)
			{
				AddLanguagePage(item2);
			}
			else
			{
				AddNewPage(item2);
			}
		}
	}

	public static void SetLanguage(LanguageCode code, bool force = false)
	{
		if (!force)
		{
			Settings.SetLanguageDropdown();
		}
		if (CurrentLanguage != code || force)
		{
			LanguageData.TryGetValue(code, out _currentLanguageFolder);
			Stationpedia.ClearAll();
			GameTips.Clear();
			if (_currentLanguageFolder != null)
			{
				_currentLanguageFolder.LoadAll();
			}
			if (CurrentLanguage != FallbackLanguage)
			{
				LanguageData.TryGetValue(FallbackLanguage, out _fallbackLanguageFolder);
				_fallbackLanguageFolder?.LoadFallback();
			}
			if (OnLanguageChanged != null)
			{
				OnLanguageChanged();
			}
			StatusUpdates.OnLanguageChanged();
		}
	}

	public static void ProcessNewPages(LanguageCode code)
	{
		if (_currentLanguageFolder != null)
		{
			_currentLanguageFolder.LoadNew(fallback: false);
		}
		if (CurrentLanguage != FallbackLanguage)
		{
			LanguageData.TryGetValue(FallbackLanguage, out _fallbackLanguageFolder);
			_fallbackLanguageFolder?.LoadNew(fallback: true);
		}
	}

	public static void Register(LocalizedText localizedText)
	{
		LocalizedInterfaces.Add(localizedText);
	}

	public static void Deregister(LocalizedText localizedText)
	{
		LocalizedInterfaces.Remove(localizedText);
	}

	public static void Register(LocalizedFont localizedText)
	{
		LocalizedFontInterfaces.Add(localizedText);
	}

	public static void Deregister(LocalizedFont localizedText)
	{
		LocalizedFontInterfaces.Remove(localizedText);
	}

	private static RegexResult GetMatches(string type, ref string masterString)
	{
		MatchCollection matchCollection = Regexes.Type(type).Matches(masterString);
		RegexResult regexResult = new RegexResult
		{
			Full = new string[matchCollection.Count],
			Names = new string[matchCollection.Count]
		};
		for (int i = 0; i < matchCollection.Count; i++)
		{
			regexResult.Full[i] = matchCollection[i].Groups[0].Value;
			regexResult.Names[i] = matchCollection[i].Groups[1].Value;
		}
		return regexResult;
	}

	private static RegexResult GetMatchesWithSpace(string type, ref string masterString)
	{
		MatchCollection matchCollection = Regexes.TypeWithSpaces(type).Matches(masterString);
		RegexResult regexResult = new RegexResult
		{
			Full = new string[matchCollection.Count],
			Names = new string[matchCollection.Count]
		};
		for (int i = 0; i < matchCollection.Count; i++)
		{
			regexResult.Full[i] = matchCollection[i].Groups[0].Value;
			regexResult.Names[i] = matchCollection[i].Groups[1].Value;
		}
		return regexResult;
	}

	private static RegexResult GetMatchesVariables(string type, ref string masterString)
	{
		MatchCollection matchCollection = Regexes.Variable(type).Matches(masterString);
		RegexResult regexResult = new RegexResult
		{
			Full = new string[matchCollection.Count],
			Names = new string[matchCollection.Count]
		};
		for (int i = 0; i < matchCollection.Count; i++)
		{
			regexResult.Full[i] = matchCollection[i].Groups[0].Value;
			regexResult.Names[i] = matchCollection[i].Groups[0].Value;
		}
		return regexResult;
	}

	private static RegexResult GetMatchesForLine(ref string masterString)
	{
		MatchCollection matchCollection = Regexes.Comment.Matches(masterString);
		RegexResult regexResult = new RegexResult
		{
			Full = new string[matchCollection.Count],
			Names = new string[matchCollection.Count]
		};
		for (int i = 0; i < matchCollection.Count; i++)
		{
			regexResult.Full[i] = matchCollection[i].Groups[0].Value;
			regexResult.Names[i] = matchCollection[i].Groups[1].Value;
		}
		return regexResult;
	}

	public static RegexResult GetMatchesForStringPreprocessing(ref string masterString)
	{
		Regex preprocessStrings = Regexes.PreprocessStrings;
		string input = Regexes.CommentLite.Replace(masterString, "");
		MatchCollection matchCollection = preprocessStrings.Matches(input);
		RegexResult regexResult = new RegexResult();
		regexResult.Full = new string[matchCollection.Count];
		regexResult.Names = new string[matchCollection.Count];
		for (int i = 0; i < matchCollection.Count; i++)
		{
			regexResult.Full[i] = matchCollection[i].Groups[0].Value;
			regexResult.Names[i] = matchCollection[i].Groups[1].Value;
		}
		return regexResult;
	}

	public static RegexResult GetMatchesForHashPreprocessing(ref string masterString)
	{
		Regex preprocessHashes = Regexes.PreprocessHashes;
		string input = Regexes.CommentLite.Replace(masterString, "");
		MatchCollection matchCollection = preprocessHashes.Matches(input);
		RegexResult regexResult = new RegexResult();
		regexResult.Full = new string[matchCollection.Count];
		regexResult.Names = new string[matchCollection.Count];
		for (int i = 0; i < matchCollection.Count; i++)
		{
			regexResult.Full[i] = matchCollection[i].Groups[0].Value;
			regexResult.Names[i] = matchCollection[i].Groups[1].Value;
		}
		return regexResult;
	}

	public static RegexResult GetMatchesForBinaryPreprocessing(ref string masterString)
	{
		Regex preprocessBinary = Regexes.PreprocessBinary;
		string input = Regexes.CommentLite.Replace(masterString, "");
		MatchCollection matchCollection = preprocessBinary.Matches(input);
		RegexResult regexResult = new RegexResult();
		regexResult.Full = new string[matchCollection.Count];
		regexResult.Names = new string[matchCollection.Count];
		for (int i = 0; i < matchCollection.Count; i++)
		{
			regexResult.Full[i] = matchCollection[i].Groups[0].Value;
			regexResult.Names[i] = matchCollection[i].Groups[1].Value;
		}
		return regexResult;
	}

	public static RegexResult GetMatchesForHexPreprocessing(ref string masterString)
	{
		Regex preprocessHex = Regexes.PreprocessHex;
		string input = Regexes.CommentLite.Replace(masterString, "");
		MatchCollection matchCollection = preprocessHex.Matches(input);
		RegexResult regexResult = new RegexResult();
		regexResult.Full = new string[matchCollection.Count];
		regexResult.Names = new string[matchCollection.Count];
		for (int i = 0; i < matchCollection.Count; i++)
		{
			regexResult.Full[i] = matchCollection[i].Groups[0].Value;
			regexResult.Names[i] = matchCollection[i].Groups[1].Value;
		}
		return regexResult;
	}

	private static RegexResult GetMatchesForNumbers(ref string masterString)
	{
		Regex numbers = Regexes.Numbers;
		string input = Regexes.CommentLite.Replace(masterString, "");
		MatchCollection matchCollection = numbers.Matches(input);
		RegexResult regexResult = new RegexResult();
		regexResult.Full = new string[matchCollection.Count];
		regexResult.Names = new string[matchCollection.Count];
		for (int i = 0; i < matchCollection.Count; i++)
		{
			regexResult.Full[i] = matchCollection[i].Groups[0].Value;
			regexResult.Names[i] = matchCollection[i].Groups[1].Value;
		}
		return regexResult;
	}

	private static RegexResult GetMatchesForNumberReferences(ref string masterString)
	{
		Regex constants = Regexes.Constants;
		string input = Regexes.CommentLite.Replace(masterString, "");
		MatchCollection matchCollection = constants.Matches(input);
		RegexResult regexResult = new RegexResult();
		regexResult.Full = new string[matchCollection.Count];
		regexResult.Names = new string[matchCollection.Count];
		for (int i = 0; i < matchCollection.Count; i++)
		{
			regexResult.Full[i] = matchCollection[i].Groups[0].Value;
			regexResult.Names[i] = matchCollection[i].Groups[1].Value;
		}
		return regexResult;
	}

	private static void ReplaceInput(ref string masterString, bool noAutoFormat = false)
	{
		RegexResult matches = GetMatches("KEY", ref masterString);
		for (int i = 0; i < matches.Count(); i++)
		{
			masterString = masterString.Replace(matches.GetFull(i), string.Format(noAutoFormat ? "{0}" : "<color=#FBB03B>{0}</color>", GetKeyName(KeyManager.GetKey(matches.GetName(i)))));
		}
	}

	private static void ReplaceThings(ref string masterString, bool noAutoFormat = false)
	{
		RegexResult matches = GetMatches("THING", ref masterString);
		for (int i = 0; i < matches.Count(); i++)
		{
			string name = matches.GetName(i);
			string thingName = GetThingName(name);
			masterString = masterString.Replace(matches.GetFull(i), string.Format(noAutoFormat ? "{0}" : "<link=Thing{1}><color=green>{0}</color></link>", thingName, name));
		}
	}

	private static void ReplaceGases(ref string masterString, bool noAutoFormat = false)
	{
		RegexResult matches = GetMatches("GAS", ref masterString);
		for (int i = 0; i < matches.Count(); i++)
		{
			string name = matches.GetName(i);
			string name2 = GetName(Mole.Create((Chemistry.GasType)Enum.Parse(typeof(Chemistry.GasType), name)).Type);
			masterString = masterString.Replace(matches.GetFull(i), string.Format(noAutoFormat ? "{0}" : "<link=Gas{1}><color=#44AD83>{0}</color></link>", name2, name));
		}
	}

	private static void ReplaceReagents(ref string masterString, bool noAutoFormat = false)
	{
		RegexResult matches = GetMatches("REAGENT", ref masterString);
		for (int i = 0; i < matches.Count(); i++)
		{
			string name = matches.GetName(i);
			masterString = masterString.Replace(matches.GetFull(i), string.Format(noAutoFormat ? "{0}" : "<link=Reagent{1}><color=#B566FF>{0}</color></link>", name.ToProper(), name));
		}
	}

	private static void ReplaceSlots(ref string masterString, bool noAutoFormat = false)
	{
		RegexResult matches = GetMatches("SLOT", ref masterString);
		for (int i = 0; i < matches.Count(); i++)
		{
			string name = matches.GetName(i);
			string slotName = GetSlotName(name);
			masterString = masterString.Replace(matches.GetFull(i), string.Format(noAutoFormat ? "{0}" : "<link=Slot{1}><color=orange>{0}</color></link>", slotName, name));
		}
	}

	public static string GetSlotTooltip(Slot.Class slotClass)
	{
		string slotName = GetSlotName(EnumCollections.SlotClasses.GetName(slotClass));
		return "<color=yellow>" + slotName + "</color>";
	}

	private static void ReplaceColors(ref string masterString)
	{
		foreach (string color in Colors)
		{
			RegexResult matchesWithSpace = GetMatchesWithSpace("COLOR" + color.ToUpper(), ref masterString);
			for (int i = 0; i < matchesWithSpace.Count(); i++)
			{
				masterString = masterString.Replace(matchesWithSpace.GetFull(i), "<color=" + color + ">" + matchesWithSpace.GetName(i) + "</color>");
			}
		}
		ParseColor("comment", "<color=#585858FF>", ref masterString);
		ParseColor("register", "<color=#0080FFFF>", ref masterString);
		ParseColor("number", "<color=#20B2AA>", ref masterString);
		ParseColor("string", "<color=white>", ref masterString);
	}

	private static void ParseColor(string word, string colorLine, ref string masterString)
	{
		RegexResult matchesWithSpace = GetMatchesWithSpace("COLOR" + word.ToUpper(), ref masterString);
		for (int i = 0; i < matchesWithSpace.Count(); i++)
		{
			masterString = masterString.Replace(matchesWithSpace.GetFull(i), colorLine + matchesWithSpace.GetName(i) + "</color>");
		}
	}

	public static string ReplaceWholeWord(this string original, string wordToFind, string replacement, EditorLineOfCode lineOfCode = null)
	{
		string[] array = original.Split('\n');
		string text = string.Empty;
		bool isUsingReference = false;
		string[] array2 = array;
		foreach (string obj in array2)
		{
			bool flag = false;
			string[] array3 = obj.Split();
			for (int j = 0; j < array3.Length; j++)
			{
				string text2 = array3[j];
				if (!flag && text2.Contains('#'))
				{
					flag = true;
				}
				if (flag || text2 != wordToFind)
				{
					text += text2;
				}
				else
				{
					isUsingReference = true;
					text += replacement;
				}
				if (j < array3.Length - 1)
				{
					text += " ";
				}
			}
			if (array.Length > 1)
			{
				text += "\n";
			}
		}
		if (lineOfCode != null)
		{
			lineOfCode.IsUsingReference = isUsingReference;
		}
		return text;
	}

	private static void ReplaceHelpHeadings(ref string masterString)
	{
		RegexResult matchesWithSpace = GetMatchesWithSpace("HEADER", ref masterString);
		for (int i = 0; i < matchesWithSpace.Count(); i++)
		{
			masterString = masterString.Replace(matchesWithSpace.GetFull(i), "<size=120%><b>" + matchesWithSpace.GetName(i) + "</b></size>");
		}
	}

	private static void ReplaceHelpMargin(ref string masterString)
	{
		RegexResult matchesWithSpace = GetMatchesWithSpace("POS", ref masterString);
		for (int i = 0; i < matchesWithSpace.Count(); i++)
		{
			masterString = masterString.Replace(matchesWithSpace.GetFull(i), "<pos=" + matchesWithSpace.GetName(i) + ">");
		}
	}

	private static void ReplaceHelpMacro(ref string masterString, string initialString, string resultString)
	{
		masterString = masterString.Replace(initialString, resultString);
	}

	private static void ReplaceHelpLinks(ref string masterString)
	{
		RegexResult matchesWithSpace = GetMatchesWithSpace("LINK", ref masterString);
		for (int i = 0; i < matchesWithSpace.Count(); i++)
		{
			string[] array = matchesWithSpace.GetName(i).Split(';');
			if (array.Length >= 2)
			{
				masterString = masterString.Replace(matchesWithSpace.GetFull(i), "<link=" + array[0] + "><color=#0080FFFF>" + array[1] + "</color></link>");
			}
		}
	}

	private static void ReplaceLogicTypes(ref string masterString)
	{
		RegexResult matchesWithSpace = GetMatchesWithSpace("LOGICTYPE", ref masterString);
		for (int i = 0; i < matchesWithSpace.Count(); i++)
		{
			masterString = masterString.Replace(matchesWithSpace.GetFull(i), string.Format("<link=LogicType{0}><color=orange>{0}</color></link>", matchesWithSpace.GetName(i)));
		}
	}

	private static void ReplaceLogicSlotTypes(ref string masterString)
	{
		RegexResult matchesWithSpace = GetMatchesWithSpace("LOGICSLOTTYPE", ref masterString);
		for (int i = 0; i < matchesWithSpace.Count(); i++)
		{
			masterString = masterString.Replace(matchesWithSpace.GetFull(i), string.Format("<link=LogicSlotType{0}><color=orange>{0}</color></link>", matchesWithSpace.GetName(i)));
		}
	}

	private static void ReplaceHelpList(ref string masterString)
	{
		RegexResult matchesWithSpace = GetMatchesWithSpace("LIST", ref masterString);
		for (int i = 0; i < matchesWithSpace.Count(); i++)
		{
			masterString = masterString.Replace(matchesWithSpace.GetFull(i), "<indent=" + matchesWithSpace.GetName(i) + ">");
		}
	}

	public static string ParseHelpText(string helpText)
	{
		ReplaceThings(ref helpText);
		ReplaceGases(ref helpText);
		ReplaceReagents(ref helpText);
		ReplaceSlots(ref helpText);
		ReplaceColors(ref helpText);
		ReplaceHelpHeadings(ref helpText);
		ReplaceHelpMargin(ref helpText);
		ReplaceHelpLinks(ref helpText);
		ReplaceHelpList(ref helpText);
		ReplaceLogicTypes(ref helpText);
		ReplaceLogicSlotTypes(ref helpText);
		ReplaceInput(ref helpText);
		ReplaceHelpMacro(ref helpText, "{LIST}", "<indent=10>");
		ReplaceHelpMacro(ref helpText, "{/LIST}", "</indent>");
		return helpText;
	}

	public static string ParseScript(string scriptText, ref List<string> acceptedStrings, ref List<string> acceptedJumps, EditorLineOfCode editorLine = null)
	{
		string[] array = scriptText.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ParseScriptLine(array[i], ref acceptedStrings, ref acceptedJumps, editorLine);
		}
		return string.Join("\n", array);
	}

	public static bool ParseDefines(List<EditorLineOfCode> code, ref List<string> acceptedStrings, ref List<string> acceptedJumps)
	{
		List<string> acceptedStrings2 = new List<string>(acceptedStrings.Count);
		List<string> acceptedJumps2 = new List<string>(acceptedJumps.Count);
		for (int i = 0; i < code.Count; i++)
		{
			ParseAliasesAndDefines(code[i].Text, ref acceptedStrings2, ref acceptedJumps2);
		}
		bool flag = false;
		if (acceptedStrings2.Count != acceptedStrings.Count)
		{
			acceptedStrings.Clear();
			acceptedStrings.AddRange(acceptedStrings2);
			flag = true;
		}
		else
		{
			for (int j = 0; j < acceptedStrings.Count; j++)
			{
				if (acceptedStrings[j] != acceptedStrings2[j])
				{
					acceptedStrings.Clear();
					acceptedStrings.AddRange(acceptedStrings2);
					flag = true;
					break;
				}
			}
		}
		bool flag2 = false;
		if (acceptedJumps2.Count != acceptedJumps.Count)
		{
			acceptedJumps.Clear();
			acceptedJumps.AddRange(acceptedJumps2);
			flag2 = true;
		}
		else
		{
			for (int k = 0; k < acceptedJumps.Count; k++)
			{
				if (acceptedJumps[k] != acceptedJumps2[k])
				{
					acceptedJumps.Clear();
					acceptedJumps.AddRange(acceptedJumps2);
					flag2 = true;
					break;
				}
			}
		}
		return flag || flag2;
	}

	public static void ParseDefines(string scriptText, ref List<string> acceptedStrings, ref List<string> acceptedJumps)
	{
		string[] array = scriptText.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			ParseAliasesAndDefines(array[i], ref acceptedStrings, ref acceptedJumps);
		}
	}

	private static string ParseScriptLine(string scriptText, ref List<string> acceptedStrings, ref List<string> acceptedJumps, EditorLineOfCode editorLine = null)
	{
		MatchCollection matchCollection = Regexes.ScriptLine.Matches(scriptText);
		string masterString = ((matchCollection.Count > 0 && matchCollection[0].Groups.Count > 1) ? matchCollection[0].Groups[1].Value : "");
		string arg = ((matchCollection.Count > 0 && matchCollection[0].Groups.Count > 2) ? matchCollection[0].Groups[2].Value : "");
		ReplaceCommands(ref masterString, ref acceptedStrings, ref acceptedJumps, editorLine);
		ReplaceNumbers(ref masterString);
		ReplaceDeviceReferences(ref masterString);
		if (matchCollection.Count > 0 && matchCollection[0].Groups.Count > 0 && !string.IsNullOrEmpty(matchCollection[0].Groups[0].Value))
		{
			scriptText = scriptText.Replace(matchCollection[0].Groups[0].Value, string.Format("{1}<color=darkgrey>{0}</color>", arg, masterString));
		}
		scriptText = scriptText.Replace("<color=darkgrey>", "<color=#585858FF>");
		scriptText = scriptText.Replace("<color=lightblue>", "<color=#0080FFFF>");
		scriptText = scriptText.Replace("<color=colornumber>", "<color=#20B2AA>");
		scriptText = scriptText.Replace("<color=colorstring>", "<color=white>");
		scriptText = scriptText.Replace("<color=colormacro>", "<color=#A0A0A0>");
		return scriptText;
	}

	private static void ReplaceDeviceReferences(ref string masterString)
	{
		MatchCollection matchCollection = Regexes.Network.Matches(masterString);
		for (int i = 0; i < matchCollection.Count; i++)
		{
			if (matchCollection[i].Groups.Count >= 1)
			{
				string value = matchCollection[i].Groups[1].Value;
				if (!string.IsNullOrEmpty(value))
				{
					masterString = masterString.Replace(":" + value, string.Format("<color=yellow>:</color>{1}{0}</color>", value, "<color=#00FFEC>"));
				}
			}
		}
		matchCollection = Regexes.Device.Matches(masterString);
		for (int j = 0; j < matchCollection.Count; j++)
		{
			if (matchCollection[j].Groups.Count >= 1)
			{
				masterString = masterString.Replace(matchCollection[j].Groups[0].Value, string.Format("<color={1}>{0}</color>", matchCollection[j].Groups[0].Value, "green"));
			}
		}
		matchCollection = Regexes.Register.Matches(masterString);
		for (int k = 0; k < matchCollection.Count; k++)
		{
			if (matchCollection[k].Groups.Count >= 1)
			{
				masterString = masterString.Replace(matchCollection[k].Groups[0].Value, string.Format("<color={1}>{0}</color>", matchCollection[k].Groups[0].Value, "lightblue"));
			}
		}
		masterString = masterString.ReplaceWholeWord("ra", string.Format("<color={1}>{0}</color>", "ra", "lightblue"));
		masterString = masterString.ReplaceWholeWord("sp", string.Format("<color={1}>{0}</color>", "sp", "lightblue"));
	}

	private static void ReplaceComments(ref string masterString)
	{
		RegexResult matchesForLine = GetMatchesForLine(ref masterString);
		for (int i = 0; i < matchesForLine.Count(); i++)
		{
			masterString = masterString.Replace(matchesForLine.GetFull(i), string.Format("<color={1}>{0}</color>", matchesForLine.GetFull(i), "darkgrey"));
		}
		masterString = masterString.Replace("<color=darkgrey>", "<color=#585858FF>");
		masterString = masterString.Replace("<color=lightblue>", "<color=#0080FFFF>");
		masterString = masterString.Replace("<color=colornumber>", "<color=#20B2AA>");
		masterString = masterString.Replace("<color=colorstring>", "<color=white>");
	}

	private static void ReplaceNumbers(ref string masterString)
	{
		RegexResult matchesForHashPreprocessing = GetMatchesForHashPreprocessing(ref masterString);
		for (int i = 0; i < matchesForHashPreprocessing.Count(); i++)
		{
			masterString = masterString.Replace(matchesForHashPreprocessing.GetFull(i), "<color=colormacro>HASH(</color><color=colorstring>\"" + matchesForHashPreprocessing.GetName(i) + "\"</color><color=colormacro>)</color>");
		}
		RegexResult matchesForStringPreprocessing = GetMatchesForStringPreprocessing(ref masterString);
		for (int j = 0; j < matchesForStringPreprocessing.Count(); j++)
		{
			masterString = masterString.Replace(matchesForStringPreprocessing.GetFull(j), "<color=colormacro>STR(</color><color=colorstring>\"" + matchesForStringPreprocessing.GetName(j) + "\"</color><color=colormacro>)</color>");
		}
		matchesForHashPreprocessing = GetMatchesForBinaryPreprocessing(ref masterString);
		for (int k = 0; k < matchesForHashPreprocessing.Count(); k++)
		{
			masterString = masterString.Replace(matchesForHashPreprocessing.GetFull(k), "<color=colornumber>" + matchesForHashPreprocessing.GetFull(k) + "</color>");
		}
		matchesForHashPreprocessing = GetMatchesForHexPreprocessing(ref masterString);
		for (int l = 0; l < matchesForHashPreprocessing.Count(); l++)
		{
			masterString = masterString.Replace(matchesForHashPreprocessing.GetFull(l), "<color=colornumber>" + matchesForHashPreprocessing.GetFull(l) + "</color>");
		}
		matchesForHashPreprocessing = GetMatchesForNumbers(ref masterString);
		for (int m = 0; m < matchesForHashPreprocessing.Count(); m++)
		{
			masterString = masterString.ReplaceWholeWord(matchesForHashPreprocessing.GetFull(m), string.Format("<color={1}>{0}</color>", matchesForHashPreprocessing.GetFull(m), "colornumber"));
		}
		matchesForHashPreprocessing = GetMatchesForNumberReferences(ref masterString);
		for (int n = 0; n < matchesForHashPreprocessing.Count(); n++)
		{
			masterString = masterString.ReplaceWholeWord(matchesForHashPreprocessing.GetFull(n), string.Format("<color={1}>{0}</color>", matchesForHashPreprocessing.GetFull(n), "colornumber"));
		}
	}

	private static void ProcessStrings(ref List<string> acceptedStrings, ref string previousDefinition, ref List<string> newDefinitions)
	{
		if (!newDefinitions.Contains(previousDefinition))
		{
			acceptedStrings.Remove(previousDefinition);
			previousDefinition = string.Empty;
		}
		else
		{
			newDefinitions.Remove(previousDefinition);
		}
		foreach (string newDefinition in newDefinitions)
		{
			acceptedStrings.Add(newDefinition);
			previousDefinition = newDefinition;
		}
	}

	private static void ParseAliasesAndDefines(string masterString, ref List<string> acceptedStrings, ref List<string> acceptedJumps)
	{
		MatchCollection commandStringA = GetCommandStringA2(masterString, "alias");
		MatchCollection commandStringA2 = GetCommandStringA2(masterString, "define");
		List<string> list = new List<string>(commandStringA.Count + commandStringA2.Count);
		for (int i = 0; i < commandStringA.Count; i++)
		{
			if (commandStringA[i].Groups.Count >= 2)
			{
				string text = commandStringA[i].Groups[1].Value.TrimEnd();
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(text);
				}
			}
		}
		for (int j = 0; j < commandStringA2.Count; j++)
		{
			if (commandStringA2[j].Groups.Count >= 2)
			{
				string text2 = commandStringA2[j].Groups[1].Value.TrimEnd();
				if (!string.IsNullOrEmpty(text2))
				{
					list.Add(text2);
				}
			}
		}
		acceptedStrings.AddRange(list);
		MatchCollection jumpString = GetJumpString(masterString);
		List<string> list2 = new List<string>(acceptedJumps.Count);
		for (int k = 0; k < jumpString.Count; k++)
		{
			if (jumpString[k].Groups.Count > 1)
			{
				string text3 = jumpString[k].Groups[1].Value.TrimEnd();
				if (!string.IsNullOrEmpty(text3))
				{
					list2.Add(text3);
				}
			}
		}
		acceptedJumps.AddRange(list2);
	}

	private static void ReplaceCommands(ref string masterString, ref List<string> acceptedStrings, ref List<string> acceptedJumps, EditorLineOfCode line = null)
	{
		if (string.IsNullOrWhiteSpace(masterString))
		{
			return;
		}
		foreach (IScriptEnum internalEnum in ProgrammableChip.InternalEnums)
		{
			internalEnum.Parse(ref masterString);
		}
		foreach (Reagent allReagent in Reagent.AllReagents)
		{
			masterString = masterString.ReplaceWholeWord(allReagent.TypeNameShort, string.Format("<color={1}><s>{0}</s></color>", allReagent.DisplayName, "orange"));
		}
		ReplaceCommandStringA2(ref masterString, "alias", "<color=yellow>{1}</color> <color=colorstring>{0}</color> {2}");
		ReplaceCommandStringA2(ref masterString, "define", "<color=yellow>{1}</color> <color=colorstring>{0}</color> {2}");
		ReplaceJumpString(ref masterString, "<color=purple>{0}</color>");
		foreach (string acceptedString in acceptedStrings)
		{
			masterString = masterString.ReplaceWholeWord(acceptedString, string.Format("<color={1}>{0}</color>", acceptedString, "colorstring"), line);
		}
		foreach (string acceptedJump in acceptedJumps)
		{
			masterString = masterString.ReplaceWholeWord(acceptedJump, string.Format("<color={1}>{0}</color>", acceptedJump, "purple"), line);
		}
		Match match = Regexes.LeadingWhitespace.Match(masterString);
		string text = string.Empty;
		if (match.Success)
		{
			text = match.Value;
		}
		masterString = masterString.TrimStart();
		foreach (ScriptCommand orderedScriptCommand in OrderedScriptCommands)
		{
			string text2 = EnumCollections.ScriptCommands.GetName(orderedScriptCommand);
			if (masterString.Length < text2.Length || !masterString.Substring(0, text2.Length).Equals(text2))
			{
				continue;
			}
			string text3 = masterString;
			if (masterString.Length < text2.Length + 1 || masterString[text2.Length] == ' ')
			{
				int spaceCount = text3.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length - 1;
				masterString = masterString.Substring(text2.Length, masterString.Length - text2.Length);
				if (LogicBase.IsDeprecated(orderedScriptCommand))
				{
					text2 = "<s>" + text2 + "</s>";
				}
				masterString = string.Format("<color={1}>{0}</color>", text2, "yellow") + masterString.TrimEnd();
				masterString = masterString + " " + ProgrammableChip.GetCommandExample(orderedScriptCommand, "darkgrey", spaceCount).Trim();
				break;
			}
		}
		masterString = text + masterString;
	}

	private static void ReplaceCommandStringA2(ref string masterString, string command, string formatString)
	{
		MatchCollection commandStringA = GetCommandStringA2(masterString, command);
		for (int i = 0; i < commandStringA.Count; i++)
		{
			if (commandStringA[i].Groups.Count >= 3)
			{
				masterString = masterString.Replace(commandStringA[i].Groups[0].Value, string.Format(formatString, commandStringA[i].Groups[1].Value, command, commandStringA[i].Groups[2].Value));
			}
		}
	}

	private static MatchCollection GetCommandStringA2(string masterString, string command)
	{
		return Regexes.CommandA2(command).Matches(masterString);
	}

	private static MatchCollection GetJumpString(string masterString)
	{
		return Regexes.JumpReferences.Matches(masterString);
	}

	private static void ReplaceJumpString(ref string masterString, string formatString)
	{
		MatchCollection matchCollection = Regexes.JumpReferences.Matches(masterString);
		for (int i = 0; i < matchCollection.Count; i++)
		{
			if (matchCollection[i].Groups.Count >= 2)
			{
				masterString = masterString.Replace(matchCollection[i].Groups[0].Value, string.Format(formatString, matchCollection[i].Groups[0].Value));
			}
		}
	}

	private static void ReplaceArguments(ref string masterString)
	{
		RegexResult matchesVariables = GetMatchesVariables("VAR1", ref masterString);
		for (int i = 0; i < matchesVariables.Count(); i++)
		{
			masterString = masterString.Replace(matchesVariables.GetFull(i), Variable1);
		}
		matchesVariables = GetMatchesVariables("VAR2", ref masterString);
		for (int j = 0; j < matchesVariables.Count(); j++)
		{
			masterString = masterString.Replace(matchesVariables.GetFull(j), Variable2);
		}
	}

	public static string ParseTooltip(string input, bool noAutoFormat = false)
	{
		Animator.StringToHash(input);
		ReplaceArguments(ref input);
		ReplaceHelpLinks(ref input);
		ReplaceGases(ref input, noAutoFormat);
		ReplaceReagents(ref input, noAutoFormat);
		ReplaceInput(ref input, noAutoFormat);
		ReplaceThings(ref input, noAutoFormat);
		ReplaceSlots(ref input, noAutoFormat);
		ReplaceColors(ref input);
		return input;
	}

	public static string GetRandomTip()
	{
		_randomTip = GameTips.Pick();
		Animator.StringToHash(_randomTip);
		if (string.IsNullOrEmpty(_randomTip))
		{
			return string.Empty;
		}
		ReplaceArguments(ref _randomTip);
		ReplaceInput(ref _randomTip);
		ReplaceGases(ref _randomTip);
		ReplaceReagents(ref _randomTip);
		ReplaceSlots(ref _randomTip);
		ReplaceThings(ref _randomTip);
		ReplaceColors(ref _randomTip);
		return _randomTip;
	}

	public static string GetKeyName(KeyCode keyCode, bool stripSpecial = true)
	{
		string name = EnumCollections.KeyCodes.GetName(keyCode);
		KeyName.TryGetValue(Animator.StringToHash(name), out var value);
		if (string.IsNullOrEmpty(value) && name != "None")
		{
			return name;
		}
		if (stripSpecial && !string.IsNullOrEmpty(value))
		{
			value = Regex.Replace(value, "\\<.*?\\>", string.Empty);
			return value.Replace("\n", "");
		}
		return value;
	}

	public static string GetThingName(string thingPrefabName)
	{
		ThingLocalized.TryGetValue(Animator.StringToHash(thingPrefabName), out var value);
		if (value == null)
		{
			FallbackThingsLocalized.TryGetValue(Animator.StringToHash(thingPrefabName), out value);
			if (value == null)
			{
				return string.Format(ErrorName, CurrentLanguage, thingPrefabName);
			}
			return value.PrefabName;
		}
		return value.PrefabName;
	}

	public static string GetThingDescription(string thingPrefabName)
	{
		ThingLocalized.TryGetValue(Animator.StringToHash(thingPrefabName), out var value);
		if (value == null || string.IsNullOrEmpty(value.PrefabName))
		{
			FallbackThingsLocalized.TryGetValue(Animator.StringToHash(thingPrefabName), out value);
		}
		if (value == null || string.IsNullOrEmpty(value.PrefabName))
		{
			return string.Format(ErrorName, CurrentLanguage, thingPrefabName);
		}
		if (!string.IsNullOrEmpty(value.Description))
		{
			return value.Description;
		}
		return Stationpedia.DefaultThingText;
	}

	public static string GetSlotName(string slotName)
	{
		SlotsName.TryGetValue(Animator.StringToHash(slotName), out var value);
		if (string.IsNullOrEmpty(value))
		{
			FallbackSlotsName.TryGetValue(Animator.StringToHash(slotName), out value);
		}
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(ErrorName, CurrentLanguage, slotName);
		}
		return value;
	}

	public static string GetName(Reagent reagent)
	{
		ReagentName.TryGetValue(reagent, out var value);
		if (string.IsNullOrEmpty(value))
		{
			FallbackReagentName.TryGetValue(reagent, out value);
		}
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(ErrorName, CurrentLanguage, reagent.GetType());
		}
		return value;
	}

	public static string GetName(Mole mole)
	{
		return GetName(mole.Type);
	}

	private static string GetParsed(int key, bool noAutoFormat, in Dictionary<int, string> textDictionary, in Dictionary<int, string> fallbackDictionary, object errorArg, string errorFormat, LanguageCode language)
	{
		textDictionary.TryGetValue(key, out var value);
		if (string.IsNullOrEmpty(value))
		{
			fallbackDictionary.TryGetValue(key, out value);
		}
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(errorFormat, language, errorArg);
		}
		return ParseTooltip(value, noAutoFormat);
	}

	private static string GetInterface(int hash, bool noAutoFormat, object errorArg, string errorFormat)
	{
		return GetParsed(hash, noAutoFormat, in InterfaceText, in FallbackInterfaceText, errorArg, errorFormat, CurrentLanguage);
	}

	public static string GetInterface(LocalizedText text, bool noAutoFormat = false)
	{
		return GetInterface(text.StringHash, noAutoFormat, text.StringKey, ErrorInterface);
	}

	public static string GetInterface(int hash, bool noAutoFormat = false)
	{
		return GetInterface(hash, noAutoFormat, hash, ErrorAction);
	}

	public static string GetInterface(string key, bool noAutoFormat = false)
	{
		return GetInterface(Animator.StringToHash(key), noAutoFormat, key, ErrorInterface);
	}

	private static string GetToolTip(int hash, bool noAutoFormat, object errorArg, string errorFormat)
	{
		return GetParsed(hash, noAutoFormat, in ToolTips, in FallbackToolTips, errorArg, errorFormat, CurrentLanguage);
	}

	public static string GetToolTip(string key, bool noAutoFormat = false)
	{
		return GetToolTip(Animator.StringToHash(key), noAutoFormat, key, ErrorInterface);
	}

	public static bool InterfaceExists(string key)
	{
		return InterfaceExists(Animator.StringToHash(key));
	}

	public static bool InterfaceExists(int hash)
	{
		return InterfaceText.ContainsKey(hash);
	}

	public static bool FallbackInterfaceExists(int hash)
	{
		if (CurrentLanguage == FallbackLanguage)
		{
			return InterfaceExists(hash);
		}
		return FallbackInterfaceText.ContainsKey(hash);
	}

	public static bool FallbackThingExists(int prefabHash)
	{
		if (CurrentLanguage == FallbackLanguage)
		{
			return ThingLocalized.ContainsKey(prefabHash);
		}
		return FallbackThingsLocalized.ContainsKey(prefabHash);
	}

	public static string GetFallbackInterface(int hash)
	{
		if (CurrentLanguage == FallbackLanguage)
		{
			return GetInterface(hash);
		}
		FallbackInterfaceText.TryGetValue(hash, out var value);
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		return ParseTooltip(value);
	}

	public static string GetFallbackInterface(string key)
	{
		if (CurrentLanguage == FallbackLanguage)
		{
			return GetInterface(key);
		}
		FallbackInterfaceText.TryGetValue(Animator.StringToHash(key), out var value);
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(ErrorInterface, FallbackLanguage, key);
		}
		return value;
	}

	public static string GetName(ColorSwatch color)
	{
		ColorNames.TryGetValue(color.StringKey, out var value);
		if (string.IsNullOrEmpty(value))
		{
			FallbackColorNames.TryGetValue(color.StringKey, out value);
		}
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(ErrorAction, CurrentLanguage, color.StringKey);
		}
		return value;
	}

	public static string GetName(Chemistry.GasType gasType)
	{
		GasName.TryGetValue(gasType, out var value);
		if (string.IsNullOrEmpty(value))
		{
			FallbackGasName.TryGetValue(gasType, out value);
		}
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(ErrorName, CurrentLanguage, Chemistry.GasType.Undefined);
		}
		return value;
	}

	public static string GetName(Thing thing, bool getFallback = false)
	{
		LocalizationThingDat value = null;
		if (!getFallback)
		{
			ThingLocalized.TryGetValue(thing.PrefabHash, out value);
		}
		if (value == null || string.IsNullOrEmpty(value.PrefabName))
		{
			FallbackThingsLocalized.TryGetValue(thing.PrefabHash, out value);
		}
		if (value == null || string.IsNullOrEmpty(value.PrefabName))
		{
			return string.Format(ErrorName, CurrentLanguage, thing.PrefabName);
		}
		return value.PrefabName;
	}

	public static string GetName(int hash, bool getFallback = false)
	{
		LocalizationThingDat value = null;
		if (!getFallback)
		{
			ThingLocalized.TryGetValue(hash, out value);
		}
		if (value == null)
		{
			return string.Empty;
		}
		if (string.IsNullOrEmpty(value.PrefabName))
		{
			FallbackThingsLocalized.TryGetValue(hash, out value);
		}
		if (string.IsNullOrEmpty(value.PrefabName))
		{
			return string.Format(ErrorName, CurrentLanguage, hash);
		}
		return value.PrefabName;
	}

	public static string GetFallbackName(Thing thing)
	{
		if (CurrentLanguage == FallbackLanguage)
		{
			return GetName(thing);
		}
		LocalizationThingDat value = null;
		FallbackThingsLocalized.TryGetValue(thing.PrefabHash, out value);
		if (value == null)
		{
			return string.Empty;
		}
		if (string.IsNullOrEmpty(value.PrefabName))
		{
			return string.Format(ErrorName, FallbackLanguage, thing.PrefabName);
		}
		return value.PrefabName;
	}

	public static string GetName(Interactable interactable)
	{
		InteractableName.TryGetValue(interactable.StringHash, out var value);
		if (string.IsNullOrEmpty(value))
		{
			FallbackInteractableName.TryGetValue(interactable.StringHash, out value);
		}
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(ErrorInteraction, CurrentLanguage, interactable.StringKey);
		}
		return value;
	}

	public static string GetName(InteractableType action)
	{
		string value = EnumCollections.InteractableTypes.GetName(action);
		int key = Animator.StringToHash(value);
		InteractableName.TryGetValue(key, out value);
		if (string.IsNullOrEmpty(value))
		{
			FallbackInteractableName.TryGetValue(key, out value);
		}
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(ErrorInteraction, CurrentLanguage, value);
		}
		return value;
	}

	public static string GetName(Slot slot)
	{
		SlotsName.TryGetValue(slot.StringHash, out var value);
		if (string.IsNullOrEmpty(value))
		{
			FallbackSlotsName.TryGetValue(slot.StringHash, out value);
		}
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(ErrorName, CurrentLanguage, slot.StringKey);
		}
		return value;
	}

	public static string GetAction(int hash)
	{
		ActionName.TryGetValue(hash, out var value);
		if (string.IsNullOrEmpty(value))
		{
			FallbackActionName.TryGetValue(hash, out value);
		}
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(ErrorAction, CurrentLanguage, hash);
		}
		return value;
	}

	public static string GetName(MinableType type)
	{
		int key = Animator.StringToHash(EnumCollections.MinableTypes.GetName(type));
		MineableName.TryGetValue(key, out var value);
		if (string.IsNullOrEmpty(value))
		{
			FallbackMineableName.TryGetValue(key, out value);
		}
		if (string.IsNullOrEmpty(value))
		{
			return string.Format(ErrorMineable, CurrentLanguage, EnumCollections.MinableTypes.GetName(type));
		}
		return value;
	}

	public static void Refresh()
	{
		foreach (LocalizedText localizedInterface in LocalizedInterfaces)
		{
			localizedInterface.Refresh();
		}
		ConsoleWindow.PrintAction($"refreshed {LocalizedInterfaces.Count} interfaces");
	}

	public static void CheckKeys()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		ConsoleWindow.PrintAction($"checking {StatusUpdates.AllStatusUpdates.Count} status updates");
		foreach (StatusUpdate allStatusUpdate in StatusUpdates.AllStatusUpdates)
		{
			if (!FallbackInterfaceExists(allStatusUpdate.StringHash))
			{
				ConsoleWindow.PrintError("missing status update string for '" + allStatusUpdate.DisplayName + "'", suppressStacktrace: true);
				num2++;
			}
		}
		ConsoleWindow.PrintAction($"checking {Prefab.AllPrefabs.Count} prefabs");
		foreach (Thing allPrefab in Prefab.AllPrefabs)
		{
			if (allPrefab.PrefabHash != Animator.StringToHash(allPrefab.PrefabName))
			{
				ConsoleWindow.PrintError("localization key invalid for prefab '" + allPrefab.PrefabName + "'", suppressStacktrace: true);
				num++;
			}
			else if (!FallbackThingExists(allPrefab.PrefabHash))
			{
				num2++;
				ConsoleWindow.PrintError("localization key missing entirely for prefab '" + allPrefab.PrefabName + "'", suppressStacktrace: true);
			}
		}
		LocalizedText[] array = UnityEngine.Object.FindObjectsOfType<LocalizedText>(includeInactive: true);
		ConsoleWindow.PrintAction($"checking {array.Length} interfaces");
		LocalizedText[] array2 = array;
		foreach (LocalizedText localizedText in array2)
		{
			if (Regex.IsMatch(localizedText.StringKey, "[^a-zA-Z]"))
			{
				ConsoleWindow.PrintError("localized interface key contains illegal characters for '" + localizedText.name + "'", suppressStacktrace: true);
				num3++;
				continue;
			}
			localizedText.Initialize();
			if (string.IsNullOrEmpty(localizedText.StringKey))
			{
				ConsoleWindow.PrintError("localized interface missing key for '" + localizedText.name + "'", suppressStacktrace: true);
				num++;
			}
			else if (!FallbackInterfaceExists(localizedText.StringHash))
			{
				ConsoleWindow.PrintError("localization key missing entirely for '" + localizedText.StringKey + "'", suppressStacktrace: true);
				num2++;
			}
		}
		if (num > 0)
		{
			ConsoleWindow.PrintAction($"broken {num} keys");
		}
		if (num2 > 0)
		{
			ConsoleWindow.PrintAction($"missing {num2} strings");
		}
		if (num == 0 && num2 == 0)
		{
			ConsoleWindow.PrintAction("no missing keys or strings");
		}
	}

	private static void AddString(StringBuilder strBuilder, string widgetStringKey, string getDefaultText, string keyName = "Record")
	{
		strBuilder.AppendLine("    <" + keyName + ">");
		strBuilder.AppendLine("        <Key>" + widgetStringKey + "</Key>");
		strBuilder.AppendLine("        <Value>" + getDefaultText + "</Value>");
		strBuilder.AppendLine("    </" + keyName + ">");
	}

	public static void CheckFont()
	{
		int num = 0;
		int num2 = 0;
		TMP_Text[] array = UnityEngine.Object.FindObjectsOfType<TMP_Text>(includeInactive: true);
		ConsoleWindow.PrintAction($"checking {array.Length} interfaces");
		TMP_Text[] array2 = array;
		foreach (TMP_Text tMP_Text in array2)
		{
			if (tMP_Text.GetComponent<LocalizedFont>() == null)
			{
				if (tMP_Text.CompareTag("NonLocalized"))
				{
					num2++;
					continue;
				}
				ConsoleWindow.PrintError("missing localized font for '" + tMP_Text.name + "' widget", suppressStacktrace: true);
				num++;
			}
		}
		ConsoleWindow.PrintAction($"missing {num} fonts");
	}

	public static void PushFont()
	{
		if (CurrentLanguage == LanguageCode.CN)
		{
			ImGui.PushFont(ImguiHelper.GetFont(2));
		}
	}

	public static void PopFont()
	{
		if (CurrentLanguage == LanguageCode.CN)
		{
			ImGui.PopFont();
		}
	}
}
