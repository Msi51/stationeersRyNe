using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIFade;

public class TooltipComponentRenderer : UiComponentRenderer
{
	public override void SetActive(bool active)
	{
		_isActive = active;
		if (LayoutElement != null)
		{
			LayoutElement.ignoreLayout = !active;
		}
		Image[] imageComponents = ImageComponents;
		foreach (Image image in imageComponents)
		{
			Color color = image.color;
			color.a = (active ? Settings.CurrentData.TooltipOpacity : 0f);
			image.color = color;
			if (_shouldDisableRaycast)
			{
				image.raycastTarget = active;
			}
		}
		TextMeshProUGUI[] textComponents = TextComponents;
		foreach (TextMeshProUGUI obj in textComponents)
		{
			Color color2 = obj.color;
			color2.a = (active ? 1 : 0);
			obj.color = color2;
		}
	}
}
