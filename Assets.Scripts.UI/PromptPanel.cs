using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class PromptPanel : UserInterfaceBase
{
	[Tooltip("Prompt texture displayed when Prompt message is triggered")]
	public Texture PromptTexture;

	[Tooltip("Component the textures will be applied to")]
	public RawImage PromptImage;

	public Button ConfirmButton;

	public TextMeshProUGUI ConfirmButtonText;

	public Button CancelButton;

	public TextMeshProUGUI CancelButtonText;

	public TextMeshProUGUI Message;

	public Image Black;

	public Image Blocker;

	public TextMeshProUGUI PromptTitleText;

	public static PromptPanel Instance;

	public GameObject PromptWindow;

	public bool IsEscapable = true;

	public bool IsActive;

	private void Awake()
	{
		Instance = this;
		PromptWindow = base.transform.GetChild(1).gameObject;
	}

	public void ShowPrompt(string title, string message, string confirmButtonText, UnityAction confirmButtonAction, bool isEscapable = true, bool hideCancelButton = false)
	{
		Message.text = message;
		PromptWindow.SetActive(value: true);
		Blocker.enabled = true;
		IsActive = true;
		ConfirmButton.onClick.RemoveAllListeners();
		CancelButton.onClick.RemoveAllListeners();
		Instance.PromptTitleText.text = title;
		ConfirmButtonText.text = confirmButtonText;
		ConfirmButton.onClick.AddListener(confirmButtonAction);
		CancelButton.gameObject.SetActive(!hideCancelButton);
		LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
		IsEscapable = isEscapable;
	}

	public void ShowPrompt(string title, string message, string confirmButtonText, UnityAction confirmButtonAction, string cancelButtonText, UnityAction cancelButtonAction, bool isEscapable = true)
	{
		Message.text = message;
		PromptWindow.SetActive(value: true);
		Blocker.enabled = true;
		IsActive = true;
		ConfirmButton.onClick.RemoveAllListeners();
		CancelButton.onClick.RemoveAllListeners();
		Instance.PromptTitleText.text = title;
		ConfirmButtonText.text = confirmButtonText;
		ConfirmButton.onClick.AddListener(confirmButtonAction);
		CancelButtonText.text = cancelButtonText;
		CancelButton.onClick.AddListener(cancelButtonAction);
		LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
		IsEscapable = isEscapable;
	}

	public async UniTask AwaitShowPrompt(string title, string message, string confirmButtonText, UnityAction confirmButtonAction, string cancelButtonText, UnityAction cancelButtonAction, bool isEscapable = true)
	{
		Message.text = message;
		PromptWindow.SetActive(value: true);
		Blocker.enabled = true;
		IsActive = true;
		ConfirmButton.onClick.RemoveAllListeners();
		CancelButton.onClick.RemoveAllListeners();
		Instance.PromptTitleText.text = title;
		ConfirmButtonText.text = confirmButtonText;
		ConfirmButton.onClick.AddListener(confirmButtonAction);
		CancelButtonText.text = cancelButtonText;
		CancelButton.onClick.AddListener(cancelButtonAction);
		LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
		IsEscapable = isEscapable;
		await UniTask.WaitUntil(() => !IsActive);
	}

	public void DisablePromptPanel()
	{
		PromptWindow.SetActive(value: false);
		Blocker.enabled = false;
		IsActive = false;
		ConfirmButton.onClick.RemoveAllListeners();
		CancelButton.onClick.RemoveAllListeners();
	}
}
