using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UI;

public class ConfirmationPanel : Singleton<ConfirmationPanel>, IModal
{
	private struct Data
	{
		public string TitleText;

		public string MessageText;

		public string Button1Text;

		public string Button2Text;

		public string Button3Text;

		public UnityAction Button1OnClick;

		public UnityAction Button2OnClick;

		public UnityAction Button3OnClick;

		public bool CloseOnEscape;
	}

	[SerializeField]
	private TextMeshProUGUI _localizedTitleText;

	[SerializeField]
	private TextMeshProUGUI _localizedMessageText;

	[SerializeField]
	private TextMeshProUGUI _button1Text;

	[SerializeField]
	private TextMeshProUGUI _button2Text;

	[SerializeField]
	private TextMeshProUGUI _button3Text;

	[SerializeField]
	private UiButton _button1;

	[SerializeField]
	private UiButton _button2;

	[SerializeField]
	private UiButton _button3;

	[SerializeField]
	private TMP_FontAsset _defaultFont;

	private readonly Stack<Data> _dataStack = new Stack<Data>();

	public bool IsVisible => base.gameObject.activeInHierarchy;

	public bool UnlockCursor => true;

	public void Show(string titleKey, string messageKey, string button1Key = null, UnityAction button1OnClick = null, string button2Key = null, UnityAction button2OnClick = null, string button3Key = null, UnityAction button3OnClick = null, bool closeOnEscape = true)
	{
		ShowRaw(GetInterfaceOrNull(titleKey), GetInterfaceOrNull(messageKey), GetInterfaceOrNull(button1Key), button1OnClick, GetInterfaceOrNull(button2Key), button2OnClick, GetInterfaceOrNull(button3Key), button3OnClick, closeOnEscape);
	}

	public void ShowWithRawMessage(string titleKey, string message, string button1Key = null, UnityAction button1OnClick = null, string button2Key = null, UnityAction button2OnClick = null, string button3Key = null, UnityAction button3OnClick = null, bool closeOnEscape = true)
	{
		ShowRaw(GetInterfaceOrNull(titleKey), message, GetInterfaceOrNull(button1Key), button1OnClick, GetInterfaceOrNull(button2Key), button2OnClick, GetInterfaceOrNull(button3Key), button3OnClick, closeOnEscape);
	}

	public void ShowWithRawTitle(string title, string messageKey, string button1Key = null, UnityAction button1OnClick = null, string button2Key = null, UnityAction button2OnClick = null, string button3Key = null, UnityAction button3OnClick = null, bool closeOnEscape = true)
	{
		ShowRaw(title, GetInterfaceOrNull(messageKey), GetInterfaceOrNull(button1Key), button1OnClick, GetInterfaceOrNull(button2Key), button2OnClick, GetInterfaceOrNull(button3Key), button3OnClick, closeOnEscape);
	}

	public void ShowRaw(string title, string message, string button1Text = null, UnityAction button1OnClick = null, string button2Text = null, UnityAction button2OnClick = null, string button3Text = null, UnityAction button3OnClick = null, bool closeOnEscape = true)
	{
		Data data = new Data
		{
			TitleText = title,
			MessageText = message,
			Button1Text = button1Text,
			Button1OnClick = button1OnClick,
			Button2Text = button2Text,
			Button2OnClick = button2OnClick,
			Button3Text = button3Text,
			Button3OnClick = button3OnClick,
			CloseOnEscape = closeOnEscape
		};
		Push(data);
	}

	public void CloseCurrentPanel()
	{
		Pop();
	}

	public void OnEscapePressed()
	{
		if (_dataStack.Peek().CloseOnEscape)
		{
			CloseCurrentPanel();
		}
	}

	private string GetInterfaceOrNull(string key)
	{
		if (key == null)
		{
			return null;
		}
		return Localization.GetInterface(key);
	}

	private void Push(Data data)
	{
		_dataStack.Push(data);
		SetUpPanel();
	}

	private void Pop()
	{
		_dataStack.Pop();
		SetUpPanel();
	}

	private void SetUpPanel()
	{
		if (_dataStack.Count < 1)
		{
			base.gameObject.SetActive(value: false);
			MouseModeController.RemoveModal(this);
			return;
		}
		Data data = _dataStack.Peek();
		TMP_FontAsset font = (Localization.CurrentFont ? Localization.CurrentFont : _defaultFont);
		_localizedTitleText.text = data.TitleText;
		_localizedTitleText.font = font;
		_localizedMessageText.text = data.MessageText;
		_localizedMessageText.font = font;
		bool flag = !string.IsNullOrEmpty(data.Button1Text);
		bool flag2 = !string.IsNullOrEmpty(data.Button2Text);
		bool flag3 = !string.IsNullOrEmpty(data.Button3Text);
		_button1.SetActive(flag);
		_button2.SetActive(flag2);
		_button3.SetActive(flag3);
		if (flag)
		{
			_button1.gameObject.SetActive(value: true);
			_button1Text.text = data.Button1Text;
			_button1Text.font = font;
			_button1.SetOnClickCallbacks(new UnityAction[2] { CloseCurrentPanel, data.Button1OnClick });
		}
		if (flag2)
		{
			_button2.gameObject.SetActive(value: true);
			_button2Text.text = data.Button2Text;
			_button2Text.font = font;
			_button2.SetOnClickCallbacks(new UnityAction[2] { CloseCurrentPanel, data.Button2OnClick });
		}
		if (flag3)
		{
			_button3.gameObject.SetActive(value: true);
			_button3Text.text = data.Button3Text;
			_button3Text.font = font;
			_button3.SetOnClickCallbacks(new UnityAction[2] { CloseCurrentPanel, data.Button3OnClick });
		}
		DisplayPanel();
	}

	private void DisplayPanel()
	{
		base.gameObject.SetActive(value: true);
		MouseModeController.AddModal(this);
	}
}
