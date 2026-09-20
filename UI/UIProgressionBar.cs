using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class UIProgressionBar : Window
{
	[Header("Progression Panel")]
	[SerializeField]
	private CanvasGroup _canvasGroup;

	[SerializeField]
	private TextMeshProUGUI _progressBarAction;

	[SerializeField]
	private TextMeshProUGUI _progressBarItemName;

	[SerializeField]
	private Slider _progressBarSlider;

	public override bool IsVisible => _canvasGroup.alpha > 0f;

	public void SetActionName(string actionName)
	{
		_progressBarAction.text = actionName;
	}

	public void SetItemName(string itemName)
	{
		_progressBarItemName.text = itemName;
	}

	public void SetProgress(float progress)
	{
		_progressBarSlider.value = progress;
	}

	public void SetProgress(float progress, string actionName, string itemDisplayName)
	{
		SetProgress(progress);
		SetActionName(actionName);
		SetItemName(itemDisplayName);
	}

	public override void SetVisible(bool isVisble)
	{
		SetActive(isVisble);
	}

	public override void SetActive(bool active)
	{
		_canvasGroup.alpha = (active ? 1 : 0);
	}
}
