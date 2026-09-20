using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSizeControl : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private RectTransform rect;

	private Vector2 oldPivot;

	private Vector2 newPivot;

	public float speed = 10f;

	private void Start()
	{
		rect = GetComponent<RectTransform>();
		oldPivot = (newPivot = rect.pivot);
	}

	private void Update()
	{
		rect.pivot = Vector2.Lerp(rect.pivot, newPivot, speed * 0.01f);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		newPivot = oldPivot - Vector2.right * 0.1f;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		newPivot = oldPivot;
	}

	public void OnPointerEnter()
	{
		newPivot = oldPivot - Vector2.right * 0.1f;
	}

	public void OnPointerExit()
	{
		newPivot = oldPivot;
	}
}
