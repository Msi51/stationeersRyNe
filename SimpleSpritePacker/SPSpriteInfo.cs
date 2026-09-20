using System;
using UnityEngine;

namespace SimpleSpritePacker;

[Serializable]
public class SPSpriteInfo : IComparable<SPSpriteInfo>
{
	public UnityEngine.Object source;

	public Sprite targetSprite;

	public string name
	{
		get
		{
			if (targetSprite != null)
			{
				return targetSprite.name;
			}
			if (source != null)
			{
				return source.name;
			}
			return string.Empty;
		}
	}

	public Vector2 sizeForComparison
	{
		get
		{
			if (source != null)
			{
				if (source is Texture2D)
				{
					return new Vector2((source as Texture2D).width, (source as Texture2D).height);
				}
				if (source is Sprite)
				{
					return (source as Sprite).rect.size;
				}
			}
			else if (targetSprite != null)
			{
				return targetSprite.rect.size;
			}
			return Vector2.zero;
		}
	}

	public int CompareTo(SPSpriteInfo other)
	{
		return name.CompareTo(other.name);
	}
}
