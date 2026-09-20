using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class AlertItem : UserInterfaceBase
{
	public TMP_Text Name;

	public Image Icon;

	public GameObject Result;

	public string AlertKey;

	[NonSerialized]
	public StatusUpdate LinkedUpdate;

	public void Assign(StatusUpdate parent)
	{
		LinkedUpdate = parent;
		Redraw();
		parent.Key = $"{parent.Type}{parent.DisplayName}";
		AlertKey = parent.Key;
		if (!StatusUpdates.AlertItems.ContainsKey(AlertKey))
		{
			StatusUpdates.AlertItems.Add(AlertKey, this);
		}
	}

	public void ButtonClick()
	{
		LinkedUpdate.IsEnabled = !LinkedUpdate.IsEnabled;
		if (LinkedUpdate.IsEnabled)
		{
			StatusUpdates.Instance.Audio.clip = LinkedUpdate.AudioAlert;
			StatusUpdates.Instance.Audio.Play();
		}
	}

	public void Redraw()
	{
		Result.SetActive(LinkedUpdate != null && LinkedUpdate.IsEnabled);
	}
}
