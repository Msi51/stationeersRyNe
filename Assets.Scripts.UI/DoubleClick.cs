using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

[RequireComponent(typeof(Button))]
public class DoubleClick : MonoBehaviour
{
	public const float DEFAULT_CLICK_SPEED = 0.5f;

	[Range(0.01f, 1f)]
	public float ClickSpeed = 0.5f;

	private float _currentClickTime;

	private bool _isSingleClick;

	public UnityEvent OnDoubleClick = new UnityEvent();

	private void Start()
	{
		GetComponent<Button>().onClick.AddListener(Click);
	}

	private void Update()
	{
		if (_isSingleClick)
		{
			_currentClickTime += Time.unscaledDeltaTime;
			if (!(_currentClickTime < ClickSpeed))
			{
				_isSingleClick = false;
				_currentClickTime = 0f;
			}
		}
	}

	private void Click()
	{
		if (!_isSingleClick)
		{
			_isSingleClick = true;
		}
		else if (!(_currentClickTime > ClickSpeed))
		{
			_currentClickTime = 0f;
			_isSingleClick = false;
			OnDoubleClick?.Invoke();
		}
	}
}
