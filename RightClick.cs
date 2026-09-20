using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RightClick : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	private Button button;

	private void Start()
	{
		button = GetComponent<Button>();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			eventData.button = PointerEventData.InputButton.Left;
			button.OnPointerClick(eventData);
		}
	}
}
