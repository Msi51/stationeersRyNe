using UnityEngine;
using UnityEngine.EventSystems;

namespace LeTai.Asset.TranslucentImage.Demo;

public class ControlCenter : MonoBehaviour
{
	public RectTransform handle;

	private RectTransform rt;

	private void Start()
	{
		rt = GetComponent<RectTransform>();
	}

	private void Update()
	{
		if (!WorldManager.IsGamePaused && !Mathf.Approximately(handle.rect.height, 0f))
		{
			rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, Mathf.Clamp(rt.anchoredPosition.y, (0f - rt.rect.height) / 2f + handle.rect.height, rt.rect.height / 2f - 1f));
		}
	}

	public void Drag(BaseEventData baseEventData)
	{
		PointerEventData pointerEventData = (PointerEventData)baseEventData;
		rt.position = new Vector2(rt.position.x, rt.position.y + pointerEventData.delta.y);
	}
}
