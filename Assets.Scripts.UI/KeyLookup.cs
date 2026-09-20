using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.UI;

public class KeyLookup
{
	public int KeyHash;

	public HashSet<KeyItem> KeyItems;

	public KeyLookup(int keyHash, KeyItem keyItem)
	{
		KeyHash = keyHash;
		KeyItems = new HashSet<KeyItem> { keyItem };
	}

	public void Add(KeyItem keyItem)
	{
		if (!KeyItems.Contains(keyItem))
		{
			KeyItems.Add(keyItem);
		}
	}

	public void Remove(KeyItem keyItem)
	{
		KeyItems.Remove(keyItem);
	}

	public void Refresh()
	{
		foreach (KeyItem keyItem in KeyItems)
		{
			bool flag = KeyManager.IsIgnoredConflict(keyItem);
			keyItem.Display.Assignment.color = ((KeyItems.Count > 1 && !flag) ? Color.red : Color.white);
			if (KeyItems.Count > 1 && keyItem.Key != KeyCode.None)
			{
				if (!ControlsAssignment.Conflicts.Contains(keyItem) && !flag)
				{
					ControlsAssignment.Conflicts.Add(keyItem);
				}
			}
			else if (ControlsAssignment.Conflicts.Contains(keyItem))
			{
				ControlsAssignment.Conflicts.Remove(keyItem);
			}
		}
		KeyManager.Instance.WarningControls.SetIsShown(ControlsAssignment.Conflicts.Count > 1);
	}
}
