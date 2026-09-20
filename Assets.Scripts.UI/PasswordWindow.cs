using System;
using Assets.Scripts.Localization2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class PasswordWindow : UserInterfaceBase
{
	[Header("Password Window")]
	[SerializeField]
	private TextMeshProUGUI _titleText;

	[SerializeField]
	private TMP_InputField _inputField;

	[SerializeField]
	private TextMeshProUGUI _placeholder;

	[SerializeField]
	private Button _confirmButton;

	[SerializeField]
	private Button _cancelButton;

	private Action<string> OnConfirm;

	private Action OnCancelled;

	private void Start()
	{
		_confirmButton.onClick.AddListener(Confirm);
		_cancelButton.onClick.AddListener(Cancel);
	}

	private void Confirm()
	{
		OnConfirm?.Invoke(_inputField.text.Trim());
		SetVisible(isVisble: false);
	}

	private void Cancel()
	{
		SetVisible(isVisble: false);
		OnCancelled?.Invoke();
	}

	public override void SetVisible(bool isVisble)
	{
		base.SetVisible(isVisble);
		if (isVisble)
		{
			_inputField.text = string.Empty;
		}
	}

	public static void PromptPassword(Action<string> onConfirm, Action onCancelled)
	{
		Prompt(GameStrings.InputPasswordTitle.DisplayString, GameStrings.InputPasswordPlaceholder.DisplayString, onConfirm, onCancelled, TMP_InputField.ContentType.Password);
	}

	public static void Prompt(string title, string placeholder, Action<string> onConfirm, Action onCancelled = null, TMP_InputField.ContentType inputType = TMP_InputField.ContentType.Alphanumeric)
	{
		PasswordWindow componentInChildren = MainMenu.Instance.GetComponentInChildren<PasswordWindow>(includeInactive: true);
		componentInChildren._titleText.text = title;
		componentInChildren._inputField.contentType = inputType;
		componentInChildren._placeholder.text = placeholder;
		componentInChildren.OnConfirm = onConfirm;
		componentInChildren.OnCancelled = onCancelled;
		componentInChildren.SetVisible(isVisble: true);
	}
}
