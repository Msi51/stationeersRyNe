using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TraderUI;

public class TabWellTab : GameBase, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField]
	private Image _selectionImage;

	[SerializeField]
	private TextMeshProUGUI _textMesh;

	private int _index;

	private TabWell _tabWell;

	private RectTransform _rectTransform;

	public bool IsEnabled { get; private set; }

	public void Initialise(int index, TabWell tabWell)
	{
		_index = index;
		_tabWell = tabWell;
		_rectTransform = Transform as RectTransform;
		IsEnabled = true;
	}

	public void SetTransform(float positionX, float width, float height)
	{
		_rectTransform.anchoredPosition = new Vector3(positionX, 0f);
		_rectTransform.sizeDelta = new Vector2(width, height);
	}

	public void SetColor(Color color)
	{
		_selectionImage.color = color;
	}

	public void SetTextColor(Color color)
	{
		_textMesh.color = color;
	}

	public void SetEnabled(bool enabled)
	{
		IsEnabled = enabled;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (IsEnabled)
		{
			_tabWell.Select(_index);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (IsEnabled)
		{
			_tabWell.PointerEnter(_index);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (IsEnabled)
		{
			_tabWell.PointerExit(_index);
		}
	}
}
