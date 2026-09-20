using UnityEngine;

namespace Assets.Scripts.UI.CustomScrollPanel;

public class ScrollPanel : MonoBehaviour
{
	[SerializeField]
	private RectTransform _contentTransform;

	[SerializeField]
	private RectTransform _viewportTransform;

	[SerializeField]
	private RectTransform _scrollBarTransform;

	[SerializeField]
	private GameObject _scrollBarGameObject;

	[SerializeField]
	private RectTransform _scrollBarHandleTransform;

	[Space(15f)]
	[SerializeField]
	private float _scrollBarWidth;

	[SerializeField]
	private float _scrollPosition;

	[SerializeField]
	private float _sensitivity;

	[SerializeField]
	private float _scrollSpeed;

	[Space(15f)]
	[SerializeField]
	private bool _pinContentToBottom;

	private float _scrollActualPosition;

	private float _scrollBarActualWidth;

	private float _viewportHeight;

	private float _contentHeight;

	private float _handleHeight;

	private float _effectiveSensitivity;

	public void SetContentHeight(float height, bool force = false)
	{
		if ((object)_contentTransform != null && (object)_viewportTransform != null && (force || !Mathf.Approximately(height, _contentTransform.sizeDelta.y) || !Mathf.Approximately(_viewportHeight, _viewportTransform.rect.size.y)))
		{
			_contentTransform.sizeDelta = new Vector2(0f, height);
			RefreshSize();
			RefreshPosition();
		}
	}

	public void OnScroll(Vector2 scrollDelta)
	{
		float y = scrollDelta.y;
		y *= -1f;
		_scrollPosition = Mathf.Clamp01(_scrollPosition + y * _effectiveSensitivity);
	}

	public void SetScrollPosition(float position)
	{
		_scrollPosition = Mathf.Clamp01(position);
		_scrollActualPosition = _scrollPosition;
		RefreshPosition();
	}

	private void RefreshSize()
	{
		_viewportHeight = _viewportTransform.rect.size.y;
		_contentHeight = _contentTransform.sizeDelta.y;
		if (_contentHeight > _viewportHeight)
		{
			_scrollBarActualWidth = _scrollBarWidth;
			_scrollBarGameObject.SetActive(value: true);
		}
		else
		{
			_scrollBarActualWidth = 0f;
			_scrollBarGameObject.SetActive(value: false);
		}
		SetScrollBarWidth();
		SetHandleSize();
	}

	private void RefreshPosition()
	{
		SetHandlePosition();
		SetContentPosition();
	}

	private void SetScrollBarWidth()
	{
		_viewportTransform.offsetMin = Vector2.zero;
		_viewportTransform.offsetMax = new Vector2(0f - _scrollBarActualWidth, 0f);
		_scrollBarTransform.anchoredPosition = Vector2.zero;
		_scrollBarTransform.sizeDelta = new Vector2(_scrollBarActualWidth, 0f);
	}

	private void SetHandleSize()
	{
		if (!(_viewportHeight <= 0f) && !(_contentHeight <= 0f))
		{
			_handleHeight = _viewportHeight * _viewportHeight / _contentHeight;
			_scrollBarHandleTransform.sizeDelta = new Vector2(0f, _handleHeight);
		}
	}

	private void SetHandlePosition()
	{
		if (_contentHeight <= _viewportHeight)
		{
			_scrollBarHandleTransform.anchoredPosition = Vector2.zero;
			return;
		}
		float num = _viewportHeight - _handleHeight;
		float num2 = _scrollActualPosition * num;
		_scrollBarHandleTransform.anchoredPosition = new Vector2(0f, 0f - num2);
	}

	private void SetContentPosition()
	{
		if (_contentHeight <= _viewportHeight)
		{
			_contentTransform.anchoredPosition = Vector2.zero;
			return;
		}
		float num = _contentHeight - _viewportHeight;
		float y = (_scrollActualPosition - (float)(_pinContentToBottom ? 1 : 0)) * num;
		_contentTransform.anchoredPosition = new Vector2(0f, y);
		_effectiveSensitivity = _sensitivity / num;
	}

	private void LateUpdate()
	{
		_scrollActualPosition = Mathf.Lerp(_scrollActualPosition, _scrollPosition, Time.deltaTime * _scrollSpeed * _sensitivity);
		if (_scrollActualPosition != _scrollPosition)
		{
			RefreshPosition();
		}
	}
}
