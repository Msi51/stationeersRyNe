using UnityEngine;
using UnityEngine.UI;

namespace ColorBlindUtility.UGUI;

[RequireComponent(typeof(Text))]
[AddComponentMenu("UI/Color-Blind Support/Color-Blind Text")]
public class ColorBlindText : ColorBlindBase
{
	public enum SupportType
	{
		ChangeColor,
		ChangeFont,
		ChangeBoth
	}

	public SupportType supportType;

	private Text text;

	public Font defaultFont;

	public Font protanopiaFont;

	public Font deuteranopiaFont;

	public Font tritanopiaFont;

	private Font FontToUse
	{
		get
		{
			switch (colorBlindMode)
			{
			case ColorBlindMode.None:
				return defaultFont;
			case ColorBlindMode.Protanopia:
				if (!protanopiaFont)
				{
					return defaultFont;
				}
				return protanopiaFont;
			case ColorBlindMode.Deuteranopia:
				if (!deuteranopiaFont)
				{
					return defaultFont;
				}
				return deuteranopiaFont;
			case ColorBlindMode.Tritanopia:
				if (!tritanopiaFont)
				{
					return defaultFont;
				}
				return tritanopiaFont;
			default:
				return defaultFont;
			}
		}
	}

	public bool IsButtonTarget
	{
		get
		{
			if (((bool)button && button.targetGraphic == text) || ((bool)GetComponent<Button>() && GetComponent<Button>().targetGraphic == GetComponent<Text>()))
			{
				return true;
			}
			return false;
		}
	}

	protected override void Start()
	{
		base.Start();
		text = GetComponent<Text>();
		defaultNormalColor = text.color;
		defaultFont = text.font;
	}

	public override void Apply(ColorBlindMode mode)
	{
		base.Apply(mode);
		if (supportType == SupportType.ChangeColor || supportType == SupportType.ChangeBoth)
		{
			if ((bool)button && button.targetGraphic == text)
			{
				ColorBlock colors = button.colors;
				colors.normalColor = base.NormalColorToUse;
				colors.disabledColor = base.DisabledColorToUse;
				colors.highlightedColor = base.HighlightedColorToUse;
				colors.pressedColor = base.PressedColorToUse;
				button.colors = colors;
			}
			text.color = base.NormalColorToUse;
		}
		if (supportType == SupportType.ChangeFont || supportType == SupportType.ChangeBoth)
		{
			text.font = FontToUse;
		}
	}
}
