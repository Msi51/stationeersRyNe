using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAudioComponent : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler
{
	public string ClickSound;

	public string HoverSound;

	public string PointerLeave;

	public bool _isHovered;

	private Toggle _toggleButton;

	private Button _button;

	public bool buttonValid = true;

	public bool DoNotPlayToggleSound;

	private Slider _slider;

	private RectTransform _rectTransform;

	private static readonly int CheckBoxOnHash = Animator.StringToHash("SFX_UI_Checkbox");

	private static readonly int CheckBoxOffHash = Animator.StringToHash("SFX_UI_CheckboxOff");

	public virtual int PointerEnterHash => Animator.StringToHash("SFX_UI_MouseOver");

	private void Start()
	{
		_rectTransform = GetComponent<RectTransform>();
		if (GetComponent<Toggle>() != null)
		{
			_toggleButton = GetComponent<Toggle>();
			_toggleButton.onValueChanged.AddListener(PlayToggleSound);
		}
		else if (GetComponent<Button>() != null)
		{
			_button = GetComponent<Button>();
		}
		else if (GetComponent<Slider>() != null)
		{
			_slider = GetComponent<Slider>();
			_slider.onValueChanged.AddListener(PlaySliderSound);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (((!_isHovered && _button != null && _button.IsInteractable()) || _toggleButton != null) && !string.IsNullOrEmpty(HoverSound))
		{
			UIAudioManager.Play(Animator.StringToHash(HoverSound));
		}
		_isHovered = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, eventData.position))
		{
			_isHovered = false;
			if (buttonValid && !string.IsNullOrEmpty(PointerLeave))
			{
				UIAudioManager.Play(Animator.StringToHash(PointerLeave));
			}
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (!string.IsNullOrEmpty(ClickSound) && _isHovered && buttonValid && ((_button != null && _button.IsInteractable()) || _toggleButton != null))
		{
			UIAudioManager.Play(Animator.StringToHash(ClickSound));
		}
	}

	public void PlayToggleSound(bool isEnabled)
	{
		if (!DoNotPlayToggleSound)
		{
			UIAudioManager.Play(_toggleButton.isOn ? CheckBoxOnHash : CheckBoxOffHash);
		}
	}

	public void PlayDropDownClickSound()
	{
		if (_isHovered)
		{
			UIAudioManager.Play(UIAudioManager.ClickLightHash);
		}
	}

	public void PlaySliderSound(float setting)
	{
		UIAudioManager.Play(UIAudioManager.SliderClickHash);
	}
}
