using System;
using Assets.Scripts;
using UnityEngine;

[Serializable]
public class ButtonReference
{
	[ReadOnly]
	public string KeyName;

	public KeyCode Key;

	public Sprite Sprite;
}
