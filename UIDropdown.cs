using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Dropdown))]
public class UIDropdown : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	private Dropdown _dropdown;

	private bool _isOpen;

	private void Awake()
	{
		_dropdown = GetComponent<Dropdown>();
		_dropdown.onValueChanged.AddListener(delegate
		{
			_isOpen = false;
		});
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
		{
			return;
		}
		_isOpen = !_isOpen;
		if (_isOpen)
		{
			Transform transform = _dropdown.transform.Find("Dropdown List");
			if ((bool)transform)
			{
				Canvas component = transform.GetComponent<Canvas>();
				component.overrideSorting = true;
				component.sortingOrder = 100;
			}
		}
		else if (!eventData.pointerPress.CompareTag("DontHideOnClick"))
		{
			_dropdown.Hide();
		}
	}
}
