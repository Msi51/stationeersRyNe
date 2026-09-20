using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorldToggle : Toggle
{
	private float lastClickTime;

	private int clickCount;

	public override void OnPointerDown(PointerEventData eventData)
	{
		OnPointerClick(eventData);
		if (clickCount == 0 || Time.time - lastClickTime <= 0.3f)
		{
			if (++clickCount == 2)
			{
				Debug.LogError("Not implimented yet");
				clickCount = 0;
			}
			lastClickTime = Time.time;
		}
		else
		{
			clickCount = 0;
		}
	}
}
