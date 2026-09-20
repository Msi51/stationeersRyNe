using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class ImageToggle : GameBase
{
	public Image ImageComponent;

	public Sprite[] Sprites = new Sprite[2];

	public bool Hidden => ImageComponent.color.a == 0f;

	public void SetImage(int index)
	{
		if (index < Sprites.Length && !(Sprites[index] == null))
		{
			ImageComponent.sprite = Sprites[index];
		}
	}

	public void HideImage(bool isHidden = true)
	{
		if (isHidden != Hidden)
		{
			ImageComponent.color = (isHidden ? Color.clear : Color.white);
		}
	}
}
