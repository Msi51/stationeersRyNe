using UnityEngine;
using UnityEngine.UI;

namespace ColorBlindUtility.UGUI;

[RequireComponent(typeof(RawImage))]
[AddComponentMenu("UI/Color-Blind Support/Color-Blind Raw Image")]
public class ColorBlindRawImage : ColorBlindBase
{
	public enum SupportType
	{
		ChangeColor,
		ChangeTexture,
		ChangeBoth
	}

	public SupportType supportType;

	private RawImage rawImage;

	public Texture defaultTexture;

	public Texture protanopiaTexture;

	public Texture deuteranopiaTexture;

	public Texture tritanopiaTexture;

	private Texture TextureToUse
	{
		get
		{
			switch (colorBlindMode)
			{
			case ColorBlindMode.None:
				return defaultTexture;
			case ColorBlindMode.Protanopia:
				if (!protanopiaTexture)
				{
					return defaultTexture;
				}
				return protanopiaTexture;
			case ColorBlindMode.Deuteranopia:
				if (!deuteranopiaTexture)
				{
					return defaultTexture;
				}
				return deuteranopiaTexture;
			case ColorBlindMode.Tritanopia:
				if (!tritanopiaTexture)
				{
					return defaultTexture;
				}
				return tritanopiaTexture;
			default:
				return defaultTexture;
			}
		}
	}

	public bool IsButtonTarget
	{
		get
		{
			if (((bool)button && button.targetGraphic == rawImage) || ((bool)GetComponent<Button>() && GetComponent<Button>().targetGraphic == GetComponent<RawImage>()))
			{
				return true;
			}
			return false;
		}
	}

	protected override void Start()
	{
		base.Start();
		rawImage = GetComponent<RawImage>();
		if ((bool)rawImage)
		{
			defaultNormalColor = rawImage.color;
			defaultTexture = rawImage.texture;
		}
	}

	public override void Apply(ColorBlindMode mode)
	{
		base.Apply(mode);
		if (supportType == SupportType.ChangeColor || supportType == SupportType.ChangeBoth)
		{
			if ((bool)button && button.targetGraphic == rawImage)
			{
				ColorBlock colors = button.colors;
				colors.normalColor = base.NormalColorToUse;
				colors.disabledColor = base.DisabledColorToUse;
				colors.highlightedColor = base.HighlightedColorToUse;
				colors.pressedColor = base.PressedColorToUse;
				button.colors = colors;
			}
			if ((bool)rawImage)
			{
				rawImage.color = base.NormalColorToUse;
			}
		}
		if ((supportType == SupportType.ChangeTexture || supportType == SupportType.ChangeBoth) && (bool)rawImage)
		{
			rawImage.texture = TextureToUse;
		}
	}
}
