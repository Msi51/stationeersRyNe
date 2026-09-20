using UnityEngine;
using UnityEngine.EventSystems;

public class DragMenu : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
{
	private bool isActive;

	private Vector2 originPos;

	private Vector2 originSize;

	public RectTransform Target;

	private Canvas myCanvas;

	private void Start()
	{
		myCanvas = GetComponentInParent<Canvas>();
	}

	private void Update()
	{
		if (!WorldManager.IsGamePaused && isActive)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(Target, Input.mousePosition, myCanvas.worldCamera, out var localPoint);
			Vector2 sizeDelta = Target.sizeDelta;
			sizeDelta.y = originSize.y - (originPos.y - localPoint.y);
			if (sizeDelta.y < 135f)
			{
				sizeDelta.y = 135f;
			}
			if (sizeDelta.y > 880f)
			{
				sizeDelta.y = 880f;
			}
			Target.sizeDelta = sizeDelta;
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		originSize = Target.sizeDelta;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(Target, Input.mousePosition, myCanvas.worldCamera, out originPos);
		isActive = true;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		PlayerPrefs.SetFloat("ChatSize", Target.sizeDelta.y);
		isActive = false;
	}
}
