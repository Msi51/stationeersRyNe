using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Serialization;
using Trading;
using UnityEngine;

public class XmlParser : MonoBehaviour
{
	private const string buyDataKeyPrefix = "BuyData";

	private const string sellDataKeyPrefix = "SellData";

	private const string TraderNameKeyPrefix = "Trader";

	public string loadPath;

	public string writePath;

	public void Process(string loadTarget, string writeTarget)
	{
		string text = Path.Combine(Application.streamingAssetsPath, loadTarget);
		string text2 = Path.Combine(Application.streamingAssetsPath, writeTarget);
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(WorldManager.GameData));
		List<Localization.Record> list = new List<Localization.Record>();
		HashSet<string> hashSet = new HashSet<string>();
		if (File.Exists(text2))
		{
			Debug.LogError("path exits " + text2);
			return;
		}
		if (File.Exists(text))
		{
			if (!(XmlSerialization.Deserialize(xmlSerializer, text) is WorldManager.GameData gameData))
			{
				Debug.LogError("Error parsing language file: " + text);
				return;
			}
			foreach (TraderData traderData in gameData.TraderDatas)
			{
				foreach (LocalizedStringReference name in traderData.Names)
				{
					string input = "Trader" + name.Value;
					input = Regex.Replace(input, "\\s+", "");
					if (!hashSet.Contains(input))
					{
						list.Add(new Localization.Record
						{
							Key = input,
							Value = name.Value
						});
						hashSet.Add(input);
					}
				}
			}
		}
		else
		{
			Debug.LogError("File not found: " + text);
		}
		Localization.Language language = new Localization.Language();
		language.Interface.AddRange(list);
		try
		{
			XmlSerializer xmlSerializer2 = new XmlSerializer(typeof(Localization.Language));
			using StreamWriter textWriter = new StreamWriter(text2);
			xmlSerializer2.Serialize(textWriter, language);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}
}
