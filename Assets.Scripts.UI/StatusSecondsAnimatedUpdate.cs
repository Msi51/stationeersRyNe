using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.UI;

[Serializable]
public class StatusSecondsAnimatedUpdate : StatusSecondsUpdate
{
	public List<Sprite> Sprites = new List<Sprite>();

	public int CurrentSprite;

	public float TimeBetweenSprites = 0.5f;

	private float _timeSinceLastSprite;

	public void UpdateSprite(float deltaTime)
	{
		if (Sprites.Count == 0)
		{
			return;
		}
		_timeSinceLastSprite += deltaTime;
		if (!(_timeSinceLastSprite < TimeBetweenSprites))
		{
			_timeSinceLastSprite = 0f;
			CurrentSprite++;
			if (CurrentSprite >= Sprites.Count)
			{
				CurrentSprite = 0;
			}
			Icon = Sprites[CurrentSprite];
			Image.sprite = Sprites[CurrentSprite];
		}
	}
}
