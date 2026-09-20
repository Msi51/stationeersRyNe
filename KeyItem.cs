using System;
using System.Xml.Serialization;
using Assets.Scripts.UI;
using UnityEngine;

[XmlRoot]
public class KeyItem
{
	[XmlElement]
	public string Name;

	[XmlElement]
	public KeyCode Key;

	[XmlIgnore]
	public KeyCode DefaultKey;

	[XmlIgnore]
	public int KeyHash;

	[XmlIgnore]
	public ControlsAssignment Display;

	[XmlIgnore]
	public int Index;

	[XmlIgnore]
	public bool Hidden;

	public event Action OnChanged;

	public KeyItem()
	{
	}

	public KeyItem(string name, KeyCode key, bool isHidden = false)
	{
		Name = name;
		Key = key;
		DefaultKey = key;
		Hidden = isHidden;
	}

	public void Changed()
	{
		this.OnChanged?.Invoke();
	}
}
