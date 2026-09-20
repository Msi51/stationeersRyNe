using System;
using UnityEngine;

namespace SimpleSpritePacker;

[Serializable]
public class SPAction
{
	public enum ActionType
	{
		Sprite_Add,
		Sprite_Remove
	}

	public ActionType actionType;

	public UnityEngine.Object resource;

	public SPSpriteInfo spriteInfo;
}
