using System;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI;

[Serializable]
public class StatusSecondsUpdate : StatusUpdate
{
	public TMP_Text Text;

	public GameObject TextObject;

	public void UpdateText(float text)
	{
		if (text <= 0f)
		{
			TextObject.SetActive(value: false);
			return;
		}
		TextObject.SetActive(value: true);
		Text.text = $"{text:F0}s";
	}
}
