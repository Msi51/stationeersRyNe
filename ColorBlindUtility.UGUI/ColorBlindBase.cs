using UnityEngine;
using UnityEngine.UI;

namespace ColorBlindUtility.UGUI;

public abstract class ColorBlindBase : MonoBehaviour
{
	protected ColorBlindMode colorBlindMode;

	protected Button button;

	public Color defaultNormalColor = Color.white;

	public Color protanopiaNormalColor = Color.white;

	public Color deuteranopiaNormalColor = Color.white;

	public Color tritanopiaNormalColor = Color.white;

	public Color defaultDisabledColor = Color.white;

	public Color protanopiaDisabledColor = Color.white;

	public Color deuteranopiaDisabledColor = Color.white;

	public Color tritanopiaDisabledColor = Color.white;

	public Color defaultHighlightedColor = Color.white;

	public Color protanopiaHighlightedColor = Color.white;

	public Color deuteranopiaHighlightedColor = Color.white;

	public Color tritanopiaHighlightedColor = Color.white;

	public Color defaultPressedColor = Color.white;

	public Color protanopiaPressedColor = Color.white;

	public Color deuteranopiaPressedColor = Color.white;

	public Color tritanopiaPressedColor = Color.white;

	protected Color NormalColorToUse => colorBlindMode switch
	{
		ColorBlindMode.None => defaultNormalColor, 
		ColorBlindMode.Protanopia => protanopiaNormalColor, 
		ColorBlindMode.Deuteranopia => deuteranopiaNormalColor, 
		ColorBlindMode.Tritanopia => tritanopiaNormalColor, 
		_ => defaultNormalColor, 
	};

	protected Color DisabledColorToUse => colorBlindMode switch
	{
		ColorBlindMode.None => defaultDisabledColor, 
		ColorBlindMode.Protanopia => protanopiaDisabledColor, 
		ColorBlindMode.Deuteranopia => deuteranopiaDisabledColor, 
		ColorBlindMode.Tritanopia => tritanopiaDisabledColor, 
		_ => defaultDisabledColor, 
	};

	protected Color HighlightedColorToUse => colorBlindMode switch
	{
		ColorBlindMode.None => defaultHighlightedColor, 
		ColorBlindMode.Protanopia => protanopiaHighlightedColor, 
		ColorBlindMode.Deuteranopia => deuteranopiaHighlightedColor, 
		ColorBlindMode.Tritanopia => tritanopiaHighlightedColor, 
		_ => defaultHighlightedColor, 
	};

	protected Color PressedColorToUse => colorBlindMode switch
	{
		ColorBlindMode.None => defaultPressedColor, 
		ColorBlindMode.Protanopia => protanopiaPressedColor, 
		ColorBlindMode.Deuteranopia => deuteranopiaPressedColor, 
		ColorBlindMode.Tritanopia => tritanopiaPressedColor, 
		_ => defaultNormalColor, 
	};

	protected virtual void Start()
	{
		button = base.transform.GetComponentInParent<Button>();
	}

	public virtual void Apply(ColorBlindMode mode)
	{
		colorBlindMode = mode;
	}
}
