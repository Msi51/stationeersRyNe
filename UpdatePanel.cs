using System.IO;
using TMPro;
using UnityEngine;

public class UpdatePanel : MonoBehaviour
{
	public TextMeshProUGUI UpdateVersion;

	public TextMeshProUGUI UpdateDate;

	public TextMeshProUGUI UpdateNotes;

	private void Start()
	{
		LoadConfig("version.ini");
	}

	public void LoadConfig(string filename)
	{
		if (!File.Exists(Application.streamingAssetsPath + "/" + filename))
		{
			return;
		}
		string text = File.ReadAllText(Application.streamingAssetsPath + "/" + filename);
		string text2 = "UPDATEVERSION=";
		int num = text.IndexOf(text2);
		if (-1 != num)
		{
			int num2 = text.IndexOf("\r", num);
			if (-1 != num2)
			{
				string sourceText = text.Substring(num + text2.Length, num2 - num - text2.Length);
				UpdateVersion.SetText(sourceText);
			}
		}
		text2 = "UPDATEDATE=";
		num = text.IndexOf(text2);
		if (-1 != num)
		{
			int num3 = text.IndexOf("\r", num);
			if (-1 != num3)
			{
				string sourceText2 = text.Substring(num + text2.Length, num3 - num - text2.Length);
				UpdateDate.SetText(sourceText2);
			}
		}
		text2 = "UPDATENOTES=";
		num = text.IndexOf(text2);
		if (-1 != num)
		{
			string sourceText3 = text.Substring(num + text2.Length, text.Length - num - text2.Length);
			UpdateNotes.SetText(sourceText3);
		}
	}
}
