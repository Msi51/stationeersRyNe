using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI;

[RequireComponent(typeof(Graphic))]
public class UISelectableGraphicBehaviour : UIBehaviour
{
	public Graphic Graphic;

	public ColorBlock ColourBlock = ColorBlock.defaultColorBlock;

	public void SetState(UiTransitionState state)
	{
		Color color = state switch
		{
			UiTransitionState.Normal => ColourBlock.normalColor, 
			UiTransitionState.Highlighted => ColourBlock.highlightedColor, 
			UiTransitionState.Pressed => ColourBlock.pressedColor, 
			UiTransitionState.Selected => ColourBlock.selectedColor, 
			UiTransitionState.Disabled => ColourBlock.disabledColor, 
			_ => throw new ArgumentOutOfRangeException("state", state, null), 
		};
		Graphic.DOColor(color * ColourBlock.colorMultiplier, ColourBlock.fadeDuration);
	}
}
