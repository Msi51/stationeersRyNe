using System;
using System.Collections.Generic;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class ControlsAssignment : UserInterfaceBase
{
	public static Dictionary<KeyCode, KeyLookup> KeyHashLookup = new Dictionary<KeyCode, KeyLookup>();

	public static List<ControlsAssignment> ControlsItems = new List<ControlsAssignment>();

	public static List<KeyLookup> KeyLookups = new List<KeyLookup>();

	public static List<KeyItem> Conflicts = new List<KeyItem>();

	public TextMeshProUGUI Name;

	public TextMeshProUGUI Assignment;

	[ReadOnly]
	public KeyItem KeyItem;

	public Button Button;

	public static void RefreshState()
	{
		foreach (KeyLookup keyLookup in KeyLookups)
		{
			keyLookup.Refresh();
		}
	}

	public static void ClearAll()
	{
		int count = ControlsItems.Count;
		while (count-- > 0)
		{
			UnityEngine.Object.Destroy(ControlsItems[count].GameObject);
		}
		ControlsItems.Clear();
		KeyLookups.Clear();
		KeyHashLookup.Clear();
		Conflicts.Clear();
	}

	public static void Register(KeyItem keyItem)
	{
		KeyHashLookup.TryGetValue(keyItem.Key, out var value);
		if (value == null)
		{
			value = new KeyLookup(keyItem.KeyHash, keyItem);
			KeyLookups.Add(value);
		}
		else
		{
			value.Add(keyItem);
		}
		KeyHashLookup[keyItem.Key] = value;
		value.Refresh();
	}

	public static void Deregister(KeyItem keyItem, bool refresh = true)
	{
		KeyHashLookup.TryGetValue(keyItem.Key, out var value);
		if (value != null)
		{
			value.Remove(keyItem);
			KeyHashLookup[keyItem.Key] = value;
			value.Refresh();
			if (refresh)
			{
				KeyManager.LoadKeyboardSetting();
			}
		}
	}

	public void Created(KeyItem keyItem, int index)
	{
		Name.text = keyItem.Name.ToProper();
		keyItem.KeyHash = Animator.StringToHash(keyItem.Name);
		keyItem.Display = this;
		KeyItem = keyItem;
		KeyItem.Index = index;
		Assign(keyItem.Key, refresh: false);
		Localization.OnLanguageChanged = (Action)Delegate.Combine(Localization.OnLanguageChanged, new Action(Refresh));
		ControlsItems.Add(this);
	}

	public void Assign(KeyCode key, bool refresh = true)
	{
		Name.text = KeyItem.Name.ToProper();
		Assignment.text = Localization.GetKeyName(key);
		KeyItem.Key = key;
		Register(KeyItem);
		if (refresh)
		{
			KeyManager.LoadKeyboardSetting();
		}
		KeyItem.Changed();
	}

	public void Refresh()
	{
		Assignment.text = Localization.GetKeyName(KeyItem.Key);
		KeyItem.Changed();
	}

	public void ButtonClick()
	{
		if (InputKeyWindow.ShowInputPanel(this))
		{
			InputKeyWindow.OnSubmit += InputKeyFinished;
		}
	}

	private void InputKeyFinished(KeyCode result)
	{
		Deregister(KeyItem);
		Assign(result);
	}
}
