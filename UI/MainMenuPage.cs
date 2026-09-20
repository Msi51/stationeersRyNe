using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class MainMenuPage : MonoBehaviour
{
	[Header("MainMenuPage")]
	[SerializeField]
	private string windowName;

	[SerializeField]
	private Button _backButton;

	public bool MenuSceneShouldBeVisible;

	public string WindowName => windowName;

	public bool IsVisible => base.gameObject.activeInHierarchy;

	private void Start()
	{
		if ((bool)_backButton)
		{
			_backButton.onClick.AddListener(OnBackPressed);
		}
	}

	public void Show()
	{
		base.gameObject.SetActive(value: true);
		OnPageShown();
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
		OnPageHidden();
	}

	private void OnBackPressed()
	{
		MainMenu.Instance.PageManager.DisableMainMenuPage(windowName);
	}

	public virtual void OnManagerStart()
	{
	}

	public virtual void OnPageShown()
	{
	}

	public virtual void OnPageHidden()
	{
	}

	public virtual void OnPagePushed()
	{
	}

	public virtual void OnPagePopped()
	{
	}
}
