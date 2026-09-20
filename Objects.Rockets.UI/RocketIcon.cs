using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Objects.Rockets.UI;

public class RocketIcon : UserInterfaceBase, IPointerClickHandler, IEventSystemHandler
{
	[Header("Rocket Icon")]
	[SerializeField]
	private Transform _rootTransform;

	[SerializeField]
	private Image _selectionHightlight;

	[SerializeField]
	private Image _image;

	[SerializeField]
	private UITooltip _tooltip;

	[Space(15f)]
	[SerializeField]
	private Color _notConnectedRocketColor;

	[SerializeField]
	private Color _notConnectedColor;

	[SerializeField]
	private Color _connectedRocketColor;

	[SerializeField]
	private Color _selectedColor;

	[SerializeField]
	private Color _notSelectedColor;

	private MapView _mapView;

	private Sprite _defaultSprite;

	private Sprite _defaultHighlight;

	private Vector2 _defaultHighlightSize;

	private bool _defaultsCached;

	public Rocket Rocket { get; set; }

	public ConnectedRocketInfo ConnectedRocketInfo { get; set; }

	public bool Connected { get; private set; }

	public bool Selected { get; private set; }

	public void Initialize(MapView mapView)
	{
		_mapView = mapView;
		CacheDefaults();
	}

	private void CacheDefaults()
	{
		if (!_defaultsCached)
		{
			_defaultsCached = true;
			_defaultSprite = _image.sprite;
			if (_selectionHightlight != null)
			{
				_defaultHighlight = _selectionHightlight.sprite;
				_defaultHighlightSize = _selectionHightlight.rectTransform.sizeDelta;
			}
		}
	}

	public void SetMapSprite(Sprite sprite, Sprite highlight)
	{
		CacheDefaults();
		_image.sprite = ((sprite != null) ? sprite : _defaultSprite);
		_image.preserveAspect = sprite != null;
		if (_selectionHightlight == null)
		{
			return;
		}
		if (highlight != null)
		{
			_selectionHightlight.sprite = highlight;
			_selectionHightlight.preserveAspect = true;
			Vector2 sizeDelta = _image.rectTransform.sizeDelta;
			if (sizeDelta.x > 1f && sizeDelta.y > 1f)
			{
				_selectionHightlight.rectTransform.sizeDelta = sizeDelta;
			}
		}
		else
		{
			_selectionHightlight.sprite = _defaultHighlight;
			_selectionHightlight.preserveAspect = false;
			_selectionHightlight.rectTransform.sizeDelta = _defaultHighlightSize;
		}
	}

	public void SetBaseScale(float scale)
	{
		Vector3 vector = Vector3.one * scale;
		_rootTransform.localScale = (Selected ? (vector * 1.2f) : vector);
	}

	public void SetState(bool selected, bool connected)
	{
		Connected = connected;
		Selected = selected;
		if (!Connected)
		{
			_image.color = _notConnectedRocketColor;
			_selectionHightlight.color = _notConnectedColor;
		}
		else
		{
			_image.color = _connectedRocketColor;
			_selectionHightlight.color = (Selected ? _selectedColor : _notSelectedColor);
		}
	}

	public void SetTooltip(string text)
	{
		if ((bool)_tooltip)
		{
			_tooltip.TooltipText = text;
		}
	}

	public void Offset(float x, float y)
	{
		_rootTransform.localPosition = new Vector3(x, y, 0f);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		ConnectedRocketInfo connectedRocketInfo = ConnectedRocketInfo;
		if (connectedRocketInfo != null && connectedRocketInfo.IsValid)
		{
			_mapView.RocketSelected(ConnectedRocketInfo);
		}
	}
}
