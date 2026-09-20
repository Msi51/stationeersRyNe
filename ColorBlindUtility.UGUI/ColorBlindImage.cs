using UnityEngine;
using UnityEngine.UI;

namespace ColorBlindUtility.UGUI;

[RequireComponent(typeof(Image))]
[AddComponentMenu("UI/Color-Blind Support/Color-Blind Image")]
public class ColorBlindImage : ColorBlindBase
{
	public enum SupportType
	{
		ChangeColor,
		ChangeSprite,
		ChangeBoth
	}

	public SupportType supportType;

	private Image image;

	public Sprite defaultSprite;

	public Sprite protanopiaSprite;

	public Sprite deuteranopiaSprite;

	public Sprite tritanopiaSprite;

	private Sprite SpriteToUse
	{
		get
		{
			switch (colorBlindMode)
			{
			case ColorBlindMode.None:
				return defaultSprite;
			case ColorBlindMode.Protanopia:
				if (!protanopiaSprite)
				{
					return defaultSprite;
				}
				return protanopiaSprite;
			case ColorBlindMode.Deuteranopia:
				if (!deuteranopiaSprite)
				{
					return defaultSprite;
				}
				return deuteranopiaSprite;
			case ColorBlindMode.Tritanopia:
				if (!tritanopiaSprite)
				{
					return defaultSprite;
				}
				return tritanopiaSprite;
			default:
				return defaultSprite;
			}
		}
	}

	public bool IsButtonTarget
	{
		get
		{
			if (((bool)button && button.targetGraphic == image) || ((bool)GetComponent<Button>() && GetComponent<Button>().targetGraphic == GetComponent<Image>()))
			{
				return true;
			}
			return false;
		}
	}

	protected override void Start()
	{
		base.Start();
		image = GetComponent<Image>();
		if ((bool)image)
		{
			defaultNormalColor = image.color;
			defaultSprite = image.sprite;
		}
	}

	public override void Apply(ColorBlindMode mode)
	{
		base.Apply(mode);
		if (supportType == SupportType.ChangeColor || supportType == SupportType.ChangeBoth)
		{
			if ((bool)button && button.targetGraphic == image)
			{
				ColorBlock colors = button.colors;
				colors.normalColor = base.NormalColorToUse;
				colors.disabledColor = base.DisabledColorToUse;
				colors.highlightedColor = base.HighlightedColorToUse;
				colors.pressedColor = base.PressedColorToUse;
				button.colors = colors;
			}
			if ((bool)image)
			{
				image.color = base.NormalColorToUse;
			}
		}
		if ((supportType == SupportType.ChangeSprite || supportType == SupportType.ChangeBoth) && (bool)image)
		{
			image.sprite = SpriteToUse;
		}
	}
}
